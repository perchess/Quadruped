﻿using System.Numerics;

namespace Quadruped
{
    public class LegPositions
    {
        public Vector3 LeftFront { get; set; }
        public Vector3 RightFront { get; set; }
        public Vector3 LeftRear { get; set; }
        public Vector3 RightRear { get; set; }

        public LegPositions(LegPositions legPositions)
        {
            LeftFront = legPositions.LeftFront;
            RightFront = legPositions.RightFront;
            LeftRear = legPositions.LeftRear;
            RightRear = legPositions.RightRear;
        }

        public LegPositions(Vector3 leftFront, Vector3 rightFront, Vector3 leftRear, Vector3 rightRear)
        {
            LeftFront = leftFront;
            RightFront = rightFront;
            LeftRear = leftRear;
            RightRear = rightRear;
        }

        public LegPositions()
        {
            LeftFront = new Vector3();
            RightFront = new Vector3();
            LeftRear = new Vector3();
            RightRear = new Vector3();
        }

        public void Transform(Vector3 transformVector, LegFlags legs = LegFlags.All)
        {
            if ((legs & LegFlags.LeftFront) != 0)
            {
                LeftFront += transformVector;
            }
            if ((legs & LegFlags.RightFront) != 0)
            {
                RightFront += transformVector;
            }
            if ((legs & LegFlags.LeftRear) != 0)
            {
                LeftRear += transformVector;
            }
            if ((legs & LegFlags.RightRear) != 0)
            {
                RightRear += transformVector;
            }
        }

        public void Rotate(Angle angle, LegFlags legs = LegFlags.All)
        {
            if ((legs & LegFlags.LeftFront) != 0)
            {
                LeftFront  = new Vector3(LeftFront.ToDirectionVector2().Rotate(angle), LeftFront.Z);
            }
            if ((legs & LegFlags.RightFront) != 0)
            {
                RightFront = new Vector3(RightFront.ToDirectionVector2().Rotate(angle), RightFront.Z);
            }
            if ((legs & LegFlags.LeftRear) != 0)
            {
                LeftRear = new Vector3(LeftRear.ToDirectionVector2().Rotate(angle), LeftRear.Z);
            }
            if ((legs & LegFlags.RightRear) != 0)
            {
                RightRear = new Vector3(RightRear.ToDirectionVector2().Rotate(angle), RightRear.Z);
            }
        }

        /// <summary>
        /// Rotate the conter of the robot by degrees
        /// </summary>
        /// <param name="rotation"></param>
        /// <param name="legs"></param>
        public void RotateCenter(Rotation rotation, LegFlags legs = LegFlags.All)
        {
            RotateCenter(Quaternion.CreateFromYawPitchRoll(rotation.Yaw.RadToDegree(), rotation.Pitch.RadToDegree(), rotation.Roll.RadToDegree()), legs);
        }

        public void RotateCenter(Quaternion rotation, LegFlags legs = LegFlags.All)
        {
            if ((legs & LegFlags.LeftFront) != 0)
            {
                LeftFront = Vector3.Transform(LeftFront, rotation);
            }
            if ((legs & LegFlags.RightFront) != 0)
            {
                RightFront = Vector3.Transform(RightFront, rotation);
            }
            if ((legs & LegFlags.LeftRear) != 0)
            {
                LeftRear = Vector3.Transform(LeftRear, rotation);
            }
            if ((legs & LegFlags.RightRear) != 0)
            {
                RightRear = Vector3.Transform(RightRear, rotation);
            }
        }

        public void MoveTowards(LegPositions target, float distance)
        {
            LeftFront = LeftFront.MoveTowards(target.LeftFront, distance);
            RightFront = RightFront.MoveTowards(target.RightFront, distance);
            LeftRear = LeftRear.MoveTowards(target.LeftRear, distance);
            RightRear = RightRear.MoveTowards(target.RightRear, distance);
        }

        public bool MoveFinished(LegPositions other)
        {
            return LeftFront == other.LeftFront &&
                   RightFront == other.RightFront &&
                   LeftRear == other.LeftRear &&
                   RightRear == other.RightRear;
        }

        public LegPositions Copy()
        {
            return new LegPositions(this);
        }

        public override string ToString()
        {
            return $"RightFront {RightFront} RightRear {RightRear} LeftFront {LeftFront} LeftRear {LeftRear}";
        }
    }
}
