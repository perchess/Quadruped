﻿using System;
using System.Linq;
using DynamixelServo.Driver;

namespace DynamixelServo.Quadruped
{
    public class QuadrupedIkDriver : IDisposable
    {
        // angles by which the legs are ofcenter from the motor centers
        private const int FemurOffset = 13;
        private const int TibiaOffset = 35;

        // Correction numbers are set up so that if you add them to the angle the motor center will bo pointing to the desired angle
        // the offset for each axis is than added to compensate for the shape of the legs
        private static readonly LegConfiguration FrontLeft  = new LegConfiguration(1, 3,  5,   45,  new Vector3(-6.5f, 6.5f, 0), -240 + FemurOffset, -330 + TibiaOffset);
        private static readonly LegConfiguration FrontRight = new LegConfiguration(2, 4,  6,  -45,  new Vector3(6.5f, 6.5f, 0),   60 + FemurOffset,  -30 + TibiaOffset);
        private static readonly LegConfiguration RearLeft   = new LegConfiguration(7, 9,  11,  135, new Vector3(-6.5f, -6.5f, 0), 60 + FemurOffset,  -30 + TibiaOffset);
        private static readonly LegConfiguration RearRight  = new LegConfiguration(8, 10, 12, -135, new Vector3(6.5f, -6.5f, 0), -240 + FemurOffset, -330 + TibiaOffset);

        private static readonly byte[] Coxas  = { FrontLeft.CoxaId, FrontRight.CoxaId, RearLeft.CoxaId, RearRight.CoxaId };
        private static readonly byte[] Femurs = { FrontLeft.FemurId, FrontRight.FemurId, RearLeft.FemurId, RearRight.FemurId };
        private static readonly byte[] Tibias = { FrontLeft.TibiaId, FrontRight.TibiaId, RearLeft.TibiaId, RearRight.TibiaId };

        private const float CoxaLength = 5.3f;
        private const float FemurLength = 6.5f;
        private const float TibiaLength = 13f;

        private readonly DynamixelDriver _driver;

        public QuadrupedIkDriver(DynamixelDriver driver)
        {
            _driver = driver;
        }

        public void Setup()
        {
            foreach (var servo in Coxas)
            {
                _driver.SetComplianceSlope(servo, ComplianceSlope.S32);
                _driver.SetMovingSpeed(servo, 300);
            }
            foreach (var servo in Femurs)
            {
                _driver.SetComplianceSlope(servo, ComplianceSlope.S32);
                _driver.SetMovingSpeed(servo, 300);
            }
            foreach (var servo in Tibias)
            {
                _driver.SetComplianceSlope(servo, ComplianceSlope.S32);
                _driver.SetMovingSpeed(servo, 300);
            }
        }

        public void RelaxedStance()
        {
            MoveLeg(new Vector3(-15,  15, -13), FrontLeft);
            MoveLeg(new Vector3(15, 15, -13), FrontRight);
            MoveLeg(new Vector3(-15, -15, -13), RearLeft);
            MoveLeg(new Vector3( 15, -15, -13), RearRight);
        }

        public void MoveLeg(Vector3 target, LegConfiguration legConfig)
        {
            var legGoalPositions = CalculateIkForLeg(target, legConfig);
            _driver.SetGoalPositionInDegrees(legConfig.CoxaId, legGoalPositions.Coxa);
            _driver.SetGoalPositionInDegrees(legConfig.FemurId, legGoalPositions.Femur);
            _driver.SetGoalPositionInDegrees(legConfig.TibiaId, legGoalPositions.Tibia);
        }

        public void MoveFrontLeftLeg(Vector3 target) => MoveLeg(target, FrontLeft);

        public void MoveFrontRightLeg(Vector3 target) => MoveLeg(target, FrontRight);

        public void MoveRearLeftLeg(Vector3 target) => MoveLeg(target, RearLeft);

        public void MoveRearRightLeg(Vector3 target) => MoveLeg(target, RearRight);

        public void DisableMotors()
        {
            foreach (var servo in Coxas)
            {
                _driver.SetTorque(servo, false);
            }
            foreach (var servo in Femurs)
            {
                _driver.SetTorque(servo, false);
            }
            foreach (var servo in Tibias)
            {
                _driver.SetTorque(servo, false);
            }
        }

        public void Dispose()
        {
            _driver?.Dispose();
        }

        private static LegGoalPositions CalculateIkForLeg(Vector3 target, LegConfiguration legConfig)
        {
            Vector3 relativeVector = target - legConfig.CoxaPosition;
            float targetAngle = (float)(Math.Atan2(relativeVector.X, relativeVector.Y).RadToDegree() + legConfig.AngleOffset);

            float horizontalDistanceToTarget = (float)Math.Sqrt(Math.Pow(relativeVector.X, 2) + Math.Pow(relativeVector.Y, 2));
            float horizontalDistanceWithoutCoxa = horizontalDistanceToTarget - CoxaLength;
            float absoluteDistanceToTargetWithoutCoxa = (float)Math.Sqrt(Math.Pow(horizontalDistanceWithoutCoxa, 2) + Math.Pow(relativeVector.Z, 2));
            // use sss triangle solution to calculate angles
            // use law of cosinus to get angles in two corners
            float angleByTibia = (float)GetAngleByA(absoluteDistanceToTargetWithoutCoxa, FemurLength, TibiaLength);
            float angleByFemur = (float)GetAngleByA(TibiaLength, FemurLength, absoluteDistanceToTargetWithoutCoxa);
            // we have angles of the SSS trianglel. now we need angle for the servos
            float groundToTargetAngleSize = (float)Math.Atan2(horizontalDistanceWithoutCoxa, -relativeVector.Z).RadToDegree();
            if (targetAngle >= 90 || targetAngle <= -90)
            {
                // target is behind me
                // can still happen if target is right bellow me
                throw new NotSupportedException($"Target angle is {targetAngle}");
            }
            float femurAngle = angleByFemur + groundToTargetAngleSize;
            // these angles need to be converted to the dynamixel angles
            // in other words horizon is 150 for the dynamixel angles so we need to recalcualate them
            float correctedFemur = Math.Abs(legConfig.FemurCorrection + femurAngle);
            float correctedTibia = Math.Abs(legConfig.TibiaCorrection + angleByTibia);
            float correctedCoxa = 150f - targetAngle;
            return new LegGoalPositions(correctedCoxa, correctedFemur, correctedTibia);
        }

        private static double GetAngleByA(double a, double b, double c)
        {
            return Math.Acos((b.ToPower(2) + c.ToPower(2) - a.ToPower(2)) / (2 * b * c)).RadToDegree();
        }

    }
}
