using System;
using System.Collections.Generic;
using RLBot.Flat;

namespace RedUtils
{
    /// <summary>
    /// Processed version of the <see cref="rlbot.flat.BallPrediction"/> that uses sane data structures.
    /// </summary>
    public struct BallPrediction
    {
        public const float RATE = 120f;
        
        public BallSlice this[int index] { get { return Slices[index]; } }

        /// <summary>A list of all of the future ball slices</summary>
        public BallSlice[] Slices;
        public int Length => Slices?.Length ?? 0;

        public BallPrediction(BallPredictionT ballPrediction)
        {
            Slices = new BallSlice[ballPrediction.Slices.Count];
            for (int i = 0; i < ballPrediction.Slices.Count; i++)
                Slices[i] = new BallSlice(ballPrediction.Slices[i]);
        }

        /// <summary>Finds the first ball slice that fits the given predicate 
        /// <para>This function is more effecient then the normal "Find" function, and accounts for scoring</para>
        /// </summary>
        public BallSlice Find(Predicate<BallSlice> predicate, int stepSize = 6)
        {
            if (Length > 0)
            {
                for (int i = stepSize; i < Length; i += stepSize)
                {
                    if (predicate(Slices[i]))
                    {
                        for (int j = i - stepSize; j < i; j++)
                        {
                            if (MathF.Abs(Slices[j].Location.y) > 5250) break;
                            if (predicate(Slices[j]))
                            {
                                return Slices[j];
                            }
                        }
                    }
                    else if (MathF.Abs(Slices[i].Location.y) > 5250) break;
                }
            }

            return null;
        }
        
        /// <summary>Finds the first ball slice that is scoring in favor of the parameter team </summary>
        public BallSlice FindGoal(int team)
        {
            int otherSide = -Field.Side(team);
            if (Length > 0)
            {
                for (int i = 6; i < Length; i += 6)
                {
                    if (Slices[i].Location.y * otherSide > 5250)
                    {
                        for (int j = i - 6; j < i; j++)
                        {
                            if (Slices[j].Location.y * otherSide > 5250) 
                                return Slices[j];
                        }
                    }
                }
            }

            return null;
        }

        public BallSlice InTime(float delta)
        {
            if (Length == 0) return null;
            
            BallSlice first = Slices[0];

            if (delta < 0) return Slices[0];
            if (delta >= 6) return Slices[^1];

            return Slices[Utils.Cap((int)(360 * delta / 6f), 0, Length - 1)];
        }
    }
}
