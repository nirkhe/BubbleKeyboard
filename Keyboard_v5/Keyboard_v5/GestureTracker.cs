using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Research.Kinect.Nui;
using Microsoft.Research.Kinect.Audio;
using Coding4Fun.Kinect.Wpf;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Keyboard_v5
{
    class GestureTracker
    {
        //private Dictionary<Joint, Microsoft.Research.Kinect.Nui.Vector> trackJoints;
        //private Dictionary<Joint, Microsoft.Research.Kinect.Nui.Vector> initialPositions;
        private Dictionary<Joint, RotationalArray<Microsoft.Research.Kinect.Nui.Vector>> positions;

        readonly int interval;
        private float xThreshold;
        private float yThreshold;
        private float zThreshold;
        private static int timer;

        private bool lockout = false;
        private Gesture guesture = null;
        private static GestureID previousGuesture = GestureID.Invalid;

        public GestureTracker(int interval, float xThreshold, float yThreshold, float zThreshold)
        {
            //trackJoints = new Dictionary<Joint, Microsoft.Research.Kinect.Nui.Vector>();
            //initialPositions = new Dictionary<Joint, Microsoft.Research.Kinect.Nui.Vector>();
            this.interval = interval;
            positions = new Dictionary<Joint, RotationalArray<Microsoft.Research.Kinect.Nui.Vector>>();
            this.xThreshold = xThreshold;
            this.yThreshold = yThreshold;
            this.zThreshold = zThreshold;
            timer = 0;
        }

        public Gesture track(SkeletonData trackSkeleton, Joint trackJoint, double elevationAngle)
        {
            timer++;
            //Guesture guesture = null;
            double theta = elevationAngle * Math.PI / 180;

            if (positions.Count < trackSkeleton.Joints.Count)
            {
                foreach (Joint j in trackSkeleton.Joints)
                {
                    if (!positions.ContainsKey(j))
                    {
                        Microsoft.Research.Kinect.Nui.Vector newPosition
                        = new Microsoft.Research.Kinect.Nui.Vector();
                        newPosition.W = j.Position.W;
                        newPosition.X = j.Position.X;
                        newPosition.Y = (float)(j.Position.Y * Math.Cos(theta) + j.Position.Z * Math.Sin(theta));
                        newPosition.Z = (float)(j.Position.Z * Math.Cos(theta) - j.Position.Y * Math.Sin(theta));

                        RotationalArray<Microsoft.Research.Kinect.Nui.Vector> jointPositions =
                            new RotationalArray<Microsoft.Research.Kinect.Nui.Vector>(interval, 0);
                        jointPositions.Add(newPosition);

                        positions.Add(j, jointPositions);
                    }
                }
            }
            else
            {
                foreach (Joint j in trackSkeleton.Joints)
                {
                    for (int i = 0; i < positions.Keys.Count; i++)
                    {
                        Joint k = positions.Keys.ElementAt(i);
                        if (j.ID == k.ID)
                        {
                            Microsoft.Research.Kinect.Nui.Vector newPosition
                               = new Microsoft.Research.Kinect.Nui.Vector();
                            newPosition.W = j.Position.W;
                            newPosition.X = j.Position.X;
                            newPosition.Y = (float)(j.Position.Y * Math.Cos(theta) + j.Position.Z * Math.Sin(theta));
                            newPosition.Z = (float)(j.Position.Z * Math.Cos(theta) - j.Position.Y * Math.Sin(theta));

                            positions[k].Add(newPosition);

                            if (k.ID == trackJoint.ID && positions[k].GetFirst().X != 0)
                            {
                                Microsoft.Research.Kinect.Nui.Vector velocityVector = new Microsoft.Research.Kinect.Nui.Vector();
                                velocityVector.X = positions[k].GetLast().X - positions[k].GetFirst().X;
                                velocityVector.Y = positions[k].GetLast().Y - positions[k].GetFirst().Y;
                                velocityVector.Z = positions[k].GetLast().Z - positions[k].GetFirst().Z;
                                velocityVector.W = positions[k].GetLast().W - positions[k].GetFirst().W;

                                guesture = guessGuesture(velocityVector, trackJoint, xThreshold, yThreshold, zThreshold);
#if DEBUG2
                                if (guesture.id != previousGuesture)
                                {
                                    Console.Out.WriteLine(guesture.id.ToString());
                                    if (guesture.id != GuestureID.Push)
                                    {
                                    }
                                }
#endif
                                if (guesture.id == previousGuesture)
                                {
                                    lockout = true;
                                }
                                else
                                {
                                    lockout = false;
                                    previousGuesture = guesture.id;
                                }
                            }

#if DEBUG
                            if (k.ID == JointID.HandLeft && !positions[k].GetFirst().X.Equals(0))
                            {
                                float x = positions[k].GetFirst().X;
                                float y = positions[k].GetLast().X;
                                float f = positions[k].GetLast().X - positions[k].GetFirst().X;
                                Debug.Print((positions[k].GetLast().X - positions[k].GetFirst().X).ToString());
                                if (Math.Abs(f) > 0.1)
                                {
                                    throw new FieldAccessException();
                                }
                            }
#endif


                            break;
                        }
                    }
                }
            }

            if (!lockout)
            {
                return guesture;
            }
            else
            {
                return new Gesture(DateTime.Now, 0, GestureID.Invalid, trackJoint);
            }
        }

        private static int MinTimerTime = 60;

        public static Gesture guessGuesture(Microsoft.Research.Kinect.Nui.Vector v, Joint source, float xThreshold, float yThreshold, float zThreshold)
        {
            timer++;
            double max = Math.Max(Math.Abs(v.X), Math.Max(Math.Abs(v.Y), Math.Abs(v.Z)));
            if (max < 0.17)
            {
                return new Gesture(DateTime.Now, 0, GestureID.Invalid, source);
            }

            if (max == Math.Abs(v.X) && max > xThreshold
               && ((previousGuesture == GestureID.SwipeLeft || previousGuesture == GestureID.SwipeRight || previousGuesture == GestureID.Invalid)
                || timer > MinTimerTime))
            {
                timer = 0;
                return (v.X > 0.0) ? new Gesture(DateTime.Now, max, GestureID.SwipeRight, source) :
                      new Gesture(DateTime.Now, max, GestureID.SwipeLeft, source);
            }
            if (timer > MinTimerTime)
            {
                if (max == Math.Abs(v.Y) && max > yThreshold)
                {
                    timer = 0;
                    return (v.Y > 0.0) ? new Gesture(DateTime.Now, max, GestureID.SwipeUp, source) :
                         new Gesture(DateTime.Now, max, GestureID.SwipeDown, source);
                }
                else if (max == Math.Abs(v.Z) && max > zThreshold)
                {
                    timer = 0;
                    return (v.Z > 0.0) ? new Gesture(DateTime.Now, max, GestureID.Pull, source) :
                         new Gesture(DateTime.Now, max, GestureID.Push, source);
                }

            }

            return new Gesture(DateTime.Now, 0, GestureID.Invalid, source);

        }
    }

    class KeyboardGestureTracker
    {

        public KeyboardGestureTracker(int interval, float xThreshold, float yThreshold, float zThreshold)
        {
            //trackJoints = new Dictionary<Joint, Microsoft.Research.Kinect.Nui.Vector>();
            //initialPositions = new Dictionary<Joint, Microsoft.Research.Kinect.Nui.Vector>();
            this.interval = interval;
            positions = new Dictionary<Joint, RotationalArray<Microsoft.Research.Kinect.Nui.Vector>>();
            this.xThreshold = xThreshold;
            this.yThreshold = yThreshold;
            this.zThreshold = zThreshold;

        }
        private int interval;
        private float xThreshold, yThreshold, zThreshold;
        private Dictionary<Joint, RotationalArray<Microsoft.Research.Kinect.Nui.Vector>> positions;
        private Gesture guesture = null;
        private GestureID previousGuestureID = GestureID.Invalid;

        public Gesture track(SkeletonData trackSkeleton, Joint trackJoint, double elevationAngle)
        {
            double theta = elevationAngle * Math.PI / 180;
            bool repeat = false;

            if (positions.Count < trackSkeleton.Joints.Count)
            {

                foreach (Joint j in trackSkeleton.Joints)
                {
                    if (!positions.ContainsKey(j))
                    {
                        Microsoft.Research.Kinect.Nui.Vector newPosition = new Microsoft.Research.Kinect.Nui.Vector();

                        newPosition.W = j.Position.W;
                        newPosition.X = j.Position.X;
                        newPosition.Y = (float)(j.Position.Y * Math.Cos(theta) + j.Position.Z * Math.Sin(theta));
                        newPosition.Z = (float)(j.Position.Z * Math.Cos(theta) - j.Position.Y * Math.Sin(theta));

                        RotationalArray<Microsoft.Research.Kinect.Nui.Vector> jointPositions =
                            new RotationalArray<Microsoft.Research.Kinect.Nui.Vector>(interval, 0);
                        jointPositions.Add(newPosition);

                        positions.Add(j, jointPositions);
                    }
                }
                guesture = new Gesture(DateTime.Now, 0, GestureID.Invalid, trackJoint);
            }
            else
            {
                foreach (Joint j in trackSkeleton.Joints)
                {
                    for (int i = 0; i < positions.Keys.Count; i++)
                    {
                        Joint k = positions.Keys.ElementAt(i);
                        if (j.ID == k.ID)
                        {
                            Microsoft.Research.Kinect.Nui.Vector newPosition = new Microsoft.Research.Kinect.Nui.Vector();

                            newPosition.W = j.Position.W;
                            newPosition.X = j.Position.X;
                            newPosition.Y = (float)(j.Position.Y * Math.Cos(theta) + j.Position.Z * Math.Sin(theta));
                            newPosition.Z = (float)(j.Position.Z * Math.Cos(theta) - j.Position.Y * Math.Sin(theta));

                            positions[k].Add(newPosition);

                            if (k.ID == trackJoint.ID)
                            {
                                Microsoft.Research.Kinect.Nui.Vector velocityVector = new Microsoft.Research.Kinect.Nui.Vector();

                                velocityVector.X = positions[k].GetLast().X - positions[k].GetFirst().X;
                                velocityVector.Y = positions[k].GetLast().Y - positions[k].GetFirst().Y;
                                velocityVector.Z = positions[k].GetLast().Z - positions[k].GetFirst().Z;
                                velocityVector.W = positions[k].GetLast().W - positions[k].GetFirst().W;

                                guesture = guessGuesture(velocityVector, trackJoint, xThreshold, yThreshold, zThreshold);
                                //Debug.Print("Guessed Guesture is :" + guesture.id.ToString());
                                if (guesture.id != GestureID.Still && guesture.id == previousGuestureID)
                                {
                                    repeat = true;
                                }
                                previousGuestureID = guesture.id;

                            }
                        }
                    }
                }
            }
            if (repeat)
            {
                return new Gesture(DateTime.Now, 0, GestureID.Invalid, trackJoint);
            }
            else
                return guesture;

        }

        public static Gesture guessGuesture(Microsoft.Research.Kinect.Nui.Vector v, Joint source, float xThreshold, float yThreshold, float zThreshold)
        {
            double max = Math.Max(Math.Abs(v.X), Math.Max(Math.Abs(v.Y), Math.Abs(v.Z)));
            if (max < 0.0)
            {
                return new Gesture(DateTime.Now, 0, GestureID.Invalid, source);
            }

            else if (Math.Abs(v.X) + Math.Abs(v.Y) <= 0.05)// Math.Abs(v.Z - ((v.X + v.Y) )) <= 0.1 )
            {
                return new Gesture(DateTime.Now, max, GestureID.Still, source);
            }

            else if ((Math.Abs(v.Z) > 2 * Math.Abs(v.X)) && (Math.Abs(v.Z) > 2 * Math.Abs(v.Y)))
            {
                return new Gesture(DateTime.Now, max, (v.Z > 0) ? GestureID.Pull : GestureID.Push, source);
            }

            else
            {
                return new Gesture(DateTime.Now, max, GestureID.Planar, source);
            }
        }

    }
}
