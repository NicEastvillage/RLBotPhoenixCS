using System;
using System.Collections.Generic;
using RedUtils.Math;
using RLBot.Flat;

namespace RedUtils
{
	/// <summary>Contains static properties on the cars currently in the game</summary>
	public static class Cars
	{
		public static Car Me;
		/// <summary>How many cars are currently in the game</summary>
		public static int Count => AllCars.Count;
		/// <summary>All the cars in the game, including cars that are respawning</summary>
		public static List<Car> AllCars { get; private set; }
		/// <summary>All the cars in the game, excluding cars that are respawning</summary>
		public static List<Car> AllLivingCars { get { return AllCars.FindAll(car => !car.IsDemolished); } }
		/// <summary>All cars on our team</summary>
		public static List<Car> AlliesAndMe { get { return AllCars.FindAll(car => car.Team == Me.Team); } }
		/// <summary>All cars on our team, excluding me</summary>
		public static List<Car> AlliesNotMe { get { return AllCars.FindAll(car => car.Team == Me.Team && car != Me); } }
		/// <summary>All cars on our team, excluding cars that are respawning</summary>
		public static List<Car> LivingAlliesAndMe { get { return AllCars.FindAll(car => car.Team == Me.Team && !car.IsDemolished); } }
		/// <summary>All cars on our team, excluding me and cars that are respawning</summary>
		public static List<Car> LivingAlliesNotMe { get { return AllCars.FindAll(car => car.Team == Me.Team && car != Me && !car.IsDemolished); } }
		/// <summary>All cars on the opponent team</summary>
		public static List<Car> Opponents { get { return AllCars.FindAll(car => car.Team != Me.Team); } }
		/// <summary>All cars on the opponent team, excluding cars that are respawning</summary>
		public static List<Car> LivingOpponents { get { return AllCars.FindAll(car => car.Team != Me.Team && !car.IsDemolished); } }
		/// <summary>All cars on the blue team, including cars that are respawning</summary>
		public static List<Car> BlueCars { get { return AllCars.FindAll(car => car.Team == 0); } }
		/// <summary>All cars on the blue team, NOT including cars that are respawning</summary>
		public static List<Car> LivingBlueCars { get { return AllCars.FindAll(car => car.Team == 0 && !car.IsDemolished); } }
		/// <summary>All cars on the orange team, including cars that are respawning</summary>
		public static List<Car> OrangeCars { get { return AllCars.FindAll(car => car.Team == 1); } }
		/// <summary>All cars on the orange team, NOT including cars that are respawning</summary>
		public static List<Car> LivingOrangeCars { get { return AllCars.FindAll(car => car.Team == 1 && !car.IsDemolished); } }

		/// <summary>Initializes the list of cars with data from the packet</summary>
		public static void Initialize(GamePacketT packet)
		{
			AllCars = new List<Car>();

			for (int i = 0; i < packet.Players.Count; i++)
			{
				AllCars.Add(new Car(i, packet.Players[i]));
			}
		}

		/// <summary>Updates the list of cars with data from the packet</summary>
		public static void Update(GamePacketT packet)
		{
			foreach (Car car in AllCars)
			{
				car.Update(packet.Players[car.Index]);
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
					result.certain = (diff > 0.1 + result.nearestCarEta / 20);
					result.nearestCar = car;
					result.nearestCarEta = eta;
				}
				
				if (eta < result.nearestEnemyEta && car.Team != Me.Team)
				{
					result.nearestEnemy = car;
					result.nearestEnemyEta = eta;
				}
				
				if (eta < result.nearestAllyEta && car.Team == Me.Team)
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
