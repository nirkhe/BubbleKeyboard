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

namespace PointAndClickKeyboard_v5
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

    class RotationalArray<T> : ICollection<T>
    {
        readonly int size;
        private int index;
        private T[] container;

        public int Count
        {
            get { return size; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public RotationalArray(int size, int startIndex)
        {
            this.size = size;
            container = new T[size];

            if (startIndex >= 0 && startIndex < size)
            {
                this.index = startIndex;
            }
            else
            {
                throw new ArgumentOutOfRangeException("startIndex", "startIndex must be greater than zero and less than size");
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return container.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)container.GetEnumerator();
        }

        public bool Contains(T item)
        {
            return container.Contains<T>(item);
        }

        public void Clear()
        {
            for (int i = container.Length - 1; i >= 0; i--)
            {
                container[i] = default(T);
            }
            return;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            container.CopyTo(array, arrayIndex);
        }

        public void Add(T item)
        {
            if (index >= size)
            {
                index = 0;
            }
            container[index] = item;
            index++;
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException("Elements in Rotational Array cannot be removed by reference");
        }

        public T GetFirst()
        {
            if (index >= size)
            {
                return container[0];
            }
            else
            {
                return container[index];
            }
        }

        public T GetLast()
        {
            if (index <= 0)
            {
                return container[size - 1];
            }
            else
            {
                return container[index - 1];
            }
        }
    }



}
