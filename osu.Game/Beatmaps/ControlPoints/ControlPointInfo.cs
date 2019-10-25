// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using osu.Framework.Lists;

namespace osu.Game.Beatmaps.ControlPoints
{
    [Serializable]
    public class ControlPointInfo
    {
        /// <summary>
        /// Control point groups.
        /// </summary>
        [JsonProperty]
        public IReadOnlyList<ControlPointGroup> Groups => groups;

        private readonly SortedList<ControlPointGroup> groups = new SortedList<ControlPointGroup>(Comparer<ControlPointGroup>.Default);

        /// <summary>
        /// All timing points.
        /// </summary>
        [JsonProperty]
        public IReadOnlyList<TimingControlPoint> TimingPoints => timingPoints;

        private readonly SortedList<TimingControlPoint> timingPoints = new SortedList<TimingControlPoint>(Comparer<TimingControlPoint>.Default);

        /// <summary>
        /// All difficulty points.
        /// </summary>
        [JsonProperty]
        public IReadOnlyList<DifficultyControlPoint> DifficultyPoints => difficultyPoints;

        private readonly SortedList<DifficultyControlPoint> difficultyPoints = new SortedList<DifficultyControlPoint>(Comparer<DifficultyControlPoint>.Default);

        /// <summary>
        /// All sound points.
        /// </summary>
        [JsonProperty]
        public IReadOnlyList<SampleControlPoint> SamplePoints => samplePoints;

        private readonly SortedList<SampleControlPoint> samplePoints = new SortedList<SampleControlPoint>(Comparer<SampleControlPoint>.Default);

        /// <summary>
        /// All effect points.
        /// </summary>
        [JsonProperty]
        public IReadOnlyList<EffectControlPoint> EffectPoints => effectPoints;

        private readonly SortedList<EffectControlPoint> effectPoints = new SortedList<EffectControlPoint>(Comparer<EffectControlPoint>.Default);

        /// <summary>
        /// All control points, of all types.
        /// </summary>
        public IEnumerable<ControlPoint> AllControlPoints => Groups.SelectMany(g => g.ControlPoints).ToArray();

        /// <summary>
        /// Finds the difficulty control point that is active at <paramref name="time"/>.
        /// </summary>
        /// <param name="time">The time to find the difficulty control point at.</param>
        /// <returns>The difficulty control point.</returns>
        public DifficultyControlPoint DifficultyPointAt(double time) => binarySearchWithFallback(DifficultyPoints, time);

        /// <summary>
        /// Finds the effect control point that is active at <paramref name="time"/>.
        /// </summary>
        /// <param name="time">The time to find the effect control point at.</param>
        /// <returns>The effect control point.</returns>
        public EffectControlPoint EffectPointAt(double time) => binarySearchWithFallback(EffectPoints, time);

        /// <summary>
        /// Finds the sound control point that is active at <paramref name="time"/>.
        /// </summary>
        /// <param name="time">The time to find the sound control point at.</param>
        /// <returns>The sound control point.</returns>
        public SampleControlPoint SamplePointAt(double time) => binarySearchWithFallback(SamplePoints, time, SamplePoints.Count > 0 ? SamplePoints[0] : null);

        /// <summary>
        /// Finds the timing control point that is active at <paramref name="time"/>.
        /// </summary>
        /// <param name="time">The time to find the timing control point at.</param>
        /// <returns>The timing control point.</returns>
        public TimingControlPoint TimingPointAt(double time) => binarySearchWithFallback(TimingPoints, time, TimingPoints.Count > 0 ? TimingPoints[0] : null);

        /// <summary>
        /// Finds the closest <see cref="ControlPoint"/> of the same type as <see cref="referencePoint"/> that is active at <paramref name="time"/>.
        /// </summary>
        /// <param name="time">The time to find the timing control point at.</param>
        /// <param name="referencePoint">A reference point to infer type.</param>
        /// <returns>The timing control point.</returns>
        public ControlPoint SimilarPointAt(double time, ControlPoint referencePoint)
        {
            switch (referencePoint)
            {
                case TimingControlPoint _: return binarySearch(TimingPoints, time);

                case EffectControlPoint _: return binarySearch(EffectPoints, time);

                case SampleControlPoint _: return binarySearch(SamplePoints, time);

                case DifficultyControlPoint _: return binarySearch(DifficultyPoints, time);
            }

            return null;
        }

        /// <summary>
        /// Finds the maximum BPM represented by any timing control point.
        /// </summary>
        [JsonIgnore]
        public double BPMMaximum =>
            60000 / (TimingPoints.OrderBy(c => c.BeatLength).FirstOrDefault() ?? new TimingControlPoint()).BeatLength;

        /// <summary>
        /// Finds the minimum BPM represented by any timing control point.
        /// </summary>
        [JsonIgnore]
        public double BPMMinimum =>
            60000 / (TimingPoints.OrderByDescending(c => c.BeatLength).FirstOrDefault() ?? new TimingControlPoint()).BeatLength;

        /// <summary>
        /// Finds the mode BPM (most common BPM) represented by the control points.
        /// </summary>
        [JsonIgnore]
        public double BPMMode =>
            60000 / (TimingPoints.GroupBy(c => c.BeatLength).OrderByDescending(grp => grp.Count()).FirstOrDefault()?.FirstOrDefault() ?? new TimingControlPoint()).BeatLength;

        /// <summary>
        /// Binary searches one of the control point lists to find the active control point at <paramref name="time"/>.
        /// Includes logic for returning a specific point when no matching point is found.
        /// </summary>
        /// <param name="list">The list to search.</param>
        /// <param name="time">The time to find the control point at.</param>
        /// <param name="prePoint">The control point to use when <paramref name="time"/> is before any control points. If null, a new control point will be constructed.</param>
        /// <returns>The active control point at <paramref name="time"/>, or a fallback <see cref="ControlPoint"/> if none found.</returns>
        private T binarySearchWithFallback<T>(IReadOnlyList<T> list, double time, T prePoint = null)
            where T : ControlPoint, new()
        {
            return binarySearch(list, time) ?? prePoint ?? new T();
        }

        /// <summary>
        /// Binary searches one of the control point lists to find the active control point at <paramref name="time"/>.
        /// </summary>
        /// <param name="list">The list to search.</param>
        /// <param name="time">The time to find the control point at.</param>
        /// <returns>The active control point at <paramref name="time"/>.</returns>
        private T binarySearch<T>(IReadOnlyList<T> list, double time)
            where T : ControlPoint
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Count == 0)
                return null;

            if (time < list[0].Time)
                return null;

            if (time >= list[list.Count - 1].Time)
                return list[list.Count - 1];

            int l = 0;
            int r = list.Count - 2;

            while (l <= r)
            {
                int pivot = l + ((r - l) >> 1);

                if (list[pivot].Time < time)
                    l = pivot + 1;
                else if (list[pivot].Time > time)
                    r = pivot - 1;
                else
                    return list[pivot];
            }

            // l will be the first control point with Time > time, but we want the one before it
            return list[l - 1];
        }

        public void Add(double time, ControlPoint newPoint, bool force = false)
        {
            if (!force && SimilarPointAt(time, newPoint)?.EquivalentTo(newPoint) == true)
                return;

            GroupAt(time, true).Add(newPoint);
        }

        public ControlPointGroup GroupAt(double time, bool createIfNotExisting)
        {
            var existing = Groups.FirstOrDefault(g => g.Time == time);

            if (existing != null)
                return existing;

            if (createIfNotExisting)
            {
                var newGroup = new ControlPointGroup(time);
                newGroup.ItemAdded += groupItemAdded;
                newGroup.ItemRemoved += groupItemRemoved;
                groups.Add(newGroup);
                return newGroup;
            }

            return null;
        }

        private void groupItemRemoved(ControlPoint obj)
        {
            switch (obj)
            {
                case TimingControlPoint typed:
                    timingPoints.Remove(typed);
                    break;

                case EffectControlPoint typed:
                    effectPoints.Remove(typed);
                    break;

                case SampleControlPoint typed:
                    samplePoints.Remove(typed);
                    break;

                case DifficultyControlPoint typed:
                    difficultyPoints.Remove(typed);
                    break;
            }
        }

        private void groupItemAdded(ControlPoint obj)
        {
            switch (obj)
            {
                case TimingControlPoint typed:
                    timingPoints.Add(typed);
                    break;

                case EffectControlPoint typed:
                    effectPoints.Add(typed);
                    break;

                case SampleControlPoint typed:
                    samplePoints.Add(typed);
                    break;

                case DifficultyControlPoint typed:
                    difficultyPoints.Add(typed);
                    break;
            }
        }

        public void Clear()
        {
            groups.Clear();
            timingPoints.Clear();
            difficultyPoints.Clear();
            samplePoints.Clear();
            effectPoints.Clear();
        }
    }
}
