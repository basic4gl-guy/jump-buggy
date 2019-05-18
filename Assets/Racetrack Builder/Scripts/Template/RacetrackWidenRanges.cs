using System;
using UnityEngine;

public class RacetrackWidenRanges : MonoBehaviour
{
    public Range Left;
    public Range Right;

    [Tooltip("Y position of guides in editor.\nFor visualisation in editor only. Has no effect on anything else.")]
    public float Y;

    public Ranges GetRanges()
    {
        return new Ranges(Left, Right); 
    }

    [Serializable]
    public struct Range
    {
        public float X0;
        public float X1;
        public float MinWidth;

        public Range(float X0, float X1, float minWidth)
        {
            this.X0 = X0; 
            this.X1 = X1;
            this.MinWidth = minWidth;
        }

        public float AdjustLeft(float x, float adjustment)
        {
            if (x >= X1)                                            // No adjustment for right of range
                return x;

            float magnitude = X1 - X0;
            float minWidth = Mathf.Min(magnitude, MinWidth);        // Effective minimum width. Don't allow to exceed magnitude (otherwise results get weird)
            adjustment = Mathf.Max(adjustment, -magnitude + minWidth);
            if (x <= X0)                                            // Full offset adjustment for left of range
                return x - adjustment;

            float scale = (magnitude + adjustment) / magnitude;     // Scale coordinates in range accordingly
            return X1 - (X1 - x) * scale;
        }

        public float AdjustRight(float x, float adjustment)
        {
            if (x <= X0)                                            // No adjustment for left of range
                return x;

            float magnitude = X1 - X0;
            float minWidth = Mathf.Min(magnitude, MinWidth);        // Effective minimum width. Don't allow to exceed magnitude (otherwise results get weird)
            adjustment = Mathf.Max(adjustment, -magnitude + minWidth);
            if (x >= X1)                                            // Full offset adjustment for right of range
                return x + adjustment;

            float scale = (magnitude + adjustment) / magnitude;     // Scale coordinates in range accordingly
            return X0 + (x - X0) * scale;
        }

        public Range Transform(Matrix4x4 M)
        {
            return new Range(TransformX(X0, M), TransformX(X1, M), TransformWidth(MinWidth, M));
        }

        private float TransformX(float x, Matrix4x4 M)
        {
            // Optimisation: I believe this could be rewritten as: M.m00*x + M.m30
            // (assuming mxx is column then row. Otherwise would be: M.m00*x + M.m03)
            // Would need testing.
            Vector4 v = new Vector4(x, 0.0f, 0.0f, 1.0f);
            v = M * v;
            return v.x;
        }

        private float TransformWidth(float w, Matrix4x4 M)
        {
            Vector4 v = new Vector4(w, 0.0f, 0.0f, 0.0f);
            v = M * v;
            return v.x;
        }

        public static readonly Range zero = new Range(0.0f, 0.0f, 0.0f);
    }

    public struct Ranges {
        public Range Left;
        public Range Right;

        public Ranges(Range left, Range right)
        {
            Left = left;
            Right = right;
        }

        /// <summary>
        /// Apply widening to X coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="widening">Left & right side widening</param>
        /// <returns>X coordinate adjusted for horizontal widening</returns>
        public float Apply(float x, RacetrackWidening widening)
        {
            x = Left.AdjustLeft(x, widening.Left);
            x = Right.AdjustRight(x, widening.Right);
            return x;
        }

        public Ranges Transform(Matrix4x4 M)
        {
            return new Ranges(Left.Transform(M), Right.Transform(M));
        }

        public static readonly Ranges zero = new Ranges(Range.zero, Range.zero);
    }
}
