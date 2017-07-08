﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using DynamixelServo.Driver;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Timer = System.Timers.Timer;

namespace DynamixelServo.TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting");
            //StartTelemetricObserver();
            using (DynamixelDriver driver = new DynamixelDriver("COM4"))
            {
                foreach (var servo in driver.Search(1, 20))
                {
                    driver.SetComplianceSlope(servo, ComplianceSlope.S128);
                    driver.SetMovingSpeed(servo, 200);
                }
                driver.SetGoalPosition(2, DynamixelDriver.DegreesToUnits(190));
                driver.SetGoalPosition(1, DynamixelDriver.DegreesToUnits(110));
                driver.SetGoalPosition(4, DynamixelDriver.DegreesToUnits(60));
                driver.SetGoalPosition(3, DynamixelDriver.DegreesToUnits(240));
                driver.SetGoalPosition(8, DynamixelDriver.DegreesToUnits(110));
                driver.SetGoalPosition(7, DynamixelDriver.DegreesToUnits(190));
                driver.SetGoalPosition(9, DynamixelDriver.DegreesToUnits(150));
                driver.SetGoalPosition(10, DynamixelDriver.DegreesToUnits(150));
                driver.SetGoalPosition(11, DynamixelDriver.DegreesToUnits(55));
                driver.SetGoalPosition(12, DynamixelDriver.DegreesToUnits(245));
                bool keepGoing = true;
                while (keepGoing)
                {
                    driver.SetGoalPosition(4, DynamixelDriver.DegreesToUnits(60));
                    driver.SetGoalPosition(3, DynamixelDriver.DegreesToUnits(240));
                    driver.SetGoalPosition(9, DynamixelDriver.DegreesToUnits(150));
                    driver.SetGoalPosition(10, DynamixelDriver.DegreesToUnits(150));
                    driver.SetGoalPosition(11, DynamixelDriver.DegreesToUnits(55));
                    driver.SetGoalPosition(12, DynamixelDriver.DegreesToUnits(245));
                    Thread.Sleep(1000);
                    driver.SetGoalPosition(4, DynamixelDriver.DegreesToUnits(70));
                    driver.SetGoalPosition(3, DynamixelDriver.DegreesToUnits(230));
                    driver.SetGoalPosition(9, DynamixelDriver.DegreesToUnits(100));
                    driver.SetGoalPosition(10, DynamixelDriver.DegreesToUnits(200));
                    driver.SetGoalPosition(11, DynamixelDriver.DegreesToUnits(95));
                    driver.SetGoalPosition(12, DynamixelDriver.DegreesToUnits(205));
                    Thread.Sleep(1000);
                    while (Console.KeyAvailable)
                    {
                        if (Console.ReadKey().Key == ConsoleKey.C)
                        {
                            keepGoing = false;
                            break;
                        }
                    }
                }
                foreach (var servo in driver.Search(1, 20))
                {
                    driver.SetTorque(servo, false);
                }
            }
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }

        public static void RecordContinuouse()
        {
            Console.WriteLine("Starting");
            using (DynamixelDriver driver = new DynamixelDriver("COM17"))
            {
                byte[] servoIds = driver.Search(1, 10);
                IList<ushort[]> history = new List<ushort[]>();
                driver.SetMovingSpeed(1, 100);
                driver.SetMovingSpeed(2, 250);
                driver.SetMovingSpeed(3, 250);
                driver.SetMovingSpeed(4, 250);
                foreach (var id in servoIds)
                {
                    driver.SetComplianceSlope(id, ComplianceSlope.S128);
                    driver.SetTorque(id, false);
                }
                Console.WriteLine("press enter to start recording");
                Console.ReadLine();
                using (Timer timer = new Timer())
                {
                    timer.Interval = 100;
                    timer.AutoReset = true;
                    timer.Elapsed += (sender, args) =>
                    {
                        ushort[] currentPositions = new ushort[servoIds.Length];
                        for (int i = 0; i < servoIds.Length; i++)
                        {
                            currentPositions[i] = driver.GetPresentPosition(servoIds[i]);
                        }
                        history.Add(currentPositions);
                    };
                    Console.WriteLine("Started recording. Press enter to stop!");
                    timer.Start();
                    Console.ReadLine();
                    timer.AutoReset = false;
                    Thread.Sleep(200);
                }
                while (true)
                {
                    foreach (var positions in history)
                    {
                        driver.MoveToAll(servoIds, positions);
                        Thread.Sleep(100);
                    }
                    Console.WriteLine("write q to exit");
                    string input = Console.ReadLine();
                    if (input == "q")
                    {
                        break;
                    }
                }
                foreach (var servoId in servoIds)
                {
                    driver.SetTorque(servoId, false);
                }
            }
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }

        public static void Record()
        {
            Console.WriteLine("Starting");
            using (DynamixelDriver driver = new DynamixelDriver("COM17"))
            {
                byte[] servoIds = driver.Search(1, 10);
                IList<ushort[]> history = new List<ushort[]>();
                driver.SetMovingSpeed(1, 100);
                driver.SetMovingSpeed(2, 200);
                driver.SetMovingSpeed(3, 200);
                driver.SetMovingSpeed(4, 200);

                foreach (var id in servoIds)
                {
                    driver.SetTorque(id, false);
                }
                Console.WriteLine("press enter to start recording");
                Console.ReadLine();
                while (true)
                {
                    ushort[] currentPositions = new ushort[servoIds.Length];
                    for (int i = 0; i < servoIds.Length; i++)
                    {
                        currentPositions[i] = driver.GetPresentPosition(servoIds[i]);
                    }
                    history.Add(currentPositions);
                    Console.WriteLine("step");
                    var input = Console.ReadLine();
                    if (input == "done")
                    {
                        break;
                    }
                }
                while (true)
                {
                    foreach (var positions in history)
                    {
                        driver.MoveToAllBlocking(servoIds, positions);
                    }
                    Console.WriteLine("write esc to exit");
                    string input = Console.ReadLine();
                    if (input == "esc")
                    {
                        break;
                    }
                }
            }
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }

        public static void StartTelemetricObserver()
        {
            Console.WriteLine("Starting");
            ConnectionFactory factory = new ConnectionFactory() { HostName = "localhost" };
            using (IConnection connection = factory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            using (DynamixelDriver driver = new DynamixelDriver("COM4"))
            {
                channel.ExchangeDeclare("DynamixelTelemetrics", "fanout");
                byte[] servos = driver.Search(1, 20);
                while (true)
                {
                    Console.WriteLine("Publising");
                    IEnumerable<ServoTelemetrics> servoData = servos.Select(servoIndex => driver.GetTelemetrics(servoIndex));
                    string json = JsonConvert.SerializeObject(servoData);
                    channel.BasicPublish("DynamixelTelemetrics", string.Empty, null, Encoding.UTF8.GetBytes(json));
                    Thread.Sleep(2 * 1000);
                }
            }
        }

        private static void ReportServoPosition()
        {
            Console.WriteLine("Starting");
            using (IConnection connection = new ConnectionFactory { HostName = "localhost" }.CreateConnection())
            using (IModel channel = connection.CreateModel())
            using (DynamixelDriver driver = new DynamixelDriver("COM4"))
            {
                channel.ExchangeDeclare("ArmPositionUpdate", "fanout");
                foreach (var servo in driver.Search(1, 5))
                {
                    driver.SetTorque(servo, false);
                }
                while (true)
                {
                    float @base = DynamixelDriver.UnitsToDegrees(driver.GetPresentPosition(1));
                    float shoulder = DynamixelDriver.UnitsToDegrees(driver.GetPresentPosition(2));
                    float elbow = DynamixelDriver.UnitsToDegrees(driver.GetPresentPosition(3));
                    float wrist = DynamixelDriver.UnitsToDegrees(driver.GetPresentPosition(4));
                    byte[] body =
                        Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new ArmPosition(@base, shoulder, elbow, wrist)));
                    channel.BasicPublish("ArmPositionUpdate", "", body: body);
                    Thread.Sleep(100);
                }

            }
        }
    }

    class ArmPosition
    {
        public float Base { get; set; }
        public float Shoulder { get; set; }
        public float Elbow { get; set; }
        public float Wrist { get; set; }

        public ArmPosition(float @base, float shoulder, float elbow, float wrist)
        {
            Base = @base;
            Shoulder = shoulder;
            Elbow = elbow;
            Wrist = wrist;
        }
    }
}
