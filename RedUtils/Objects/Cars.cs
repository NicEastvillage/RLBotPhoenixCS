using System;
using System.Collections.Generic;
using rlbot.flat;
using RedUtils.Math;

namespace RedUtils
{
	/// <summary>Contains static properties on the cars currently in the game</summary>
	public static class Cars
	{
		/// <summary>How many cars are currently in the game</summary>
		public static int Count => AllCars.Count;
		/// <summary>All the cars in the game, including cars that are respawning</summary>
		public static List<Car> AllCars { get; private set; }
		/// <summary>All the cars in the game, NOT including cars that are respawning</summary>
		public static List<Car> AllLivingCars { get { return AllCars.FindAll(car => !car.IsDemolished); } }
		/// <summary>All cars on the blue team, including cars that are respawning</summary>
		public static List<Car> BlueCars { get { return AllCars.FindAll(car => car.Team == 0); } }
		/// <summary>All cars on the blue team, NOT including cars that are respawning</summary>
		public static List<Car> LivingBlueCars { get { return AllCars.FindAll(car => !car.IsDemolished && car.Team == 0); } }
		/// <summary>All cars on the orange team, including cars that are respawning</summary>
		public static List<Car> OrangeCars { get { return AllCars.FindAll(car => car.Team == 1); } }
		/// <summary>All cars on the orange team, NOT including cars that are respawning</summary>
		public static List<Car> LivingOrangeCars { get { return AllCars.FindAll(car => !car.IsDemolished && car.Team == 1); } }

		/// <summary>Initializes the list of cars with data from the packet</summary>
		public static void Initialize(GameTickPacket packet)
		{
			AllCars = new List<Car>();

			for (int i = 0; i < packet.PlayersLength; i++)
			{
				AllCars.Add(new Car(i, packet.Players(i).Value));
			}
		}

		/// <summary>Updates the list of cars with data from the packet</summary>
		public static void Update(GameTickPacket packet)
		{
			foreach (Car car in AllCars)
			{
				car.Update(packet.Players(car.Index).Value);
			}
		}
		
		public static NearestCarsByEtaData NearestCarsByEta()
		{
			var result = new NearestCarsByEtaData();
			foreach (Car car in AllLivingCars)
			{
				float roughEta = car.Location.Dist(Ball.Location) / 2100f;
				float eta = Drive.GetEta(car, Ball.Prediction[System.Math.Min((int)(roughEta / 60), 359)].Location);
				
				if (eta < result.nearestCarEta)
				{
					float diff = result.nearestCarEta - eta;
					result.certain = (diff > 0.05 + result.nearestCarEta / 20);
					result.nearestCar = car;
					result.nearestCarEta = eta;
				}
				
				if (eta < result.nearestEnemyEta)
				{
					result.nearestEnemy = car;
					result.nearestEnemyEta = eta;
				}
				
				if (eta < result.nearestAllyEta)
				{
					result.nearestAlly = car;
					result.nearestAllyEta = eta;
				}
			}

			return result;
		}
	}
	
	public class NearestCarsByEtaData
	{
		public Car nearestCar = null;
		public float nearestCarEta = 10000f;
		public Car nearestEnemy = null;
		public float nearestEnemyEta = 10000f;
		public Car nearestAlly = null;
		public float nearestAllyEta = 10000f;
		public bool certain = true;
	}
}
