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

namespace ControlCompWithBubbleKeyboard
{
    class KeyboardGuestureTracker
    {

        public KeyboardGuestureTracker(int interval, float xThreshold, float yThreshold, float zThreshold)
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
        private Guesture guesture = null;
        private GuestureID previousGuestureID = GuestureID.Invalid;

        public Guesture track(SkeletonData trackSkeleton, Joint trackJoint, double elevationAngle)
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
                guesture = new Guesture(DateTime.Now, 0, GuestureID.Invalid, trackJoint);
            }
            else
            {
                foreach(Joint j in trackSkeleton.Joints)
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
                                if (guesture.id != GuestureID.Still && guesture.id == previousGuestureID)
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
                return new Guesture(DateTime.Now, 0, GuestureID.Invalid, trackJoint);
            }
            else
                return guesture;
            
        }

        public static Guesture guessGuesture(Microsoft.Research.Kinect.Nui.Vector v, Joint source, float xThreshold, float yThreshold, float zThreshold)
        {
            double max = Math.Max(Math.Abs(v.X), Math.Max(Math.Abs(v.Y), Math.Abs(v.Z)));
            if (max < 0.0)
            {
                return new Guesture(DateTime.Now, 0, GuestureID.Invalid, source);
            }

            else if (Math.Abs(v.X) + Math.Abs(v.Y) <= 0.05)// Math.Abs(v.Z - ((v.X + v.Y) )) <= 0.1 )
            {
                return new Guesture(DateTime.Now, max, GuestureID.Still, source);
            }

            else if ( (Math.Abs(v.Z) > 2 * Math.Abs(v.X)) && (Math.Abs(v.Z) > 2* Math.Abs(v.Y)) )
            {
                return new Guesture(DateTime.Now, max, (v.Z > 0) ? GuestureID.Pull : GuestureID.Push, source);
            }

            else
            {
                return new Guesture(DateTime.Now, max, GuestureID.Planar, source);
            }
        }

    }
}
