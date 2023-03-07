using Microsoft.Xna.Framework;

namespace OctoDash
{
    public class CatmullRomSpline
    {
        /** Calculates the catmullrom value for the given position (t).
         * @param out The Vector to set to the result.
         * @param t The position (0<=t<=1) on the spline
         * @param points The control points
         * @param continuous If true the b-spline restarts at 0 when reaching 1
         * @param tmp A temporary vector used for the calculation
         * @return The value of out */
        public static Vector2 calculate(Vector2 _out, float t, Vector2[] points, bool continuous,
            Vector2 tmp)
        {
            int n = continuous ? points.Length : points.Length - 3;
            float u = t * n;
            int i = (t >= 1f) ? (n - 1) : (int)u;
            u -= i;
            return calculate(_out, i, u, points, continuous, tmp);
        }

        /** Calculates the catmullrom value for the given span (i) at the given position (u).
         * @param out The Vector to set to the result.
         * @param i The span (0<=i<spanCount) spanCount = continuous ? points.length : points.length - degree
         * @param u The position (0<=u<=1) on the span
         * @param points The control points
         * @param continuous If true the b-spline restarts at 0 when reaching 1
         * @param tmp A temporary vector used for the calculation
         * @return The value of out */
        public static Vector2 calculate(Vector2 _out, int i, float u, Vector2[] points,
            bool continuous, Vector2 tmp)
        {
            int n = points.Length;
            float u2 = u * u;
            float u3 = u2 * u;
            _out = points[i];
            _out *= 1.5f * u3 - 2.5f * u2 + 1.0f;
            if (continuous || i > 0)
            {
                tmp = points[(n + i - 1) % n] * (-0.5f * u3 + u2 - 0.5f * u);
                _out += tmp;
            }
            if (continuous || i < (n - 1))
            {
                tmp = points[(i + 1) % n] * (-1.5f * u3 + 2f * u2 + 0.5f * u);
                _out += tmp;
            }
            if (continuous || i < (n - 2))
            {
                tmp = points[(i + 2) % n] * (0.5f * u3 - 0.5f * u2);
                _out += tmp;
            }
            return _out;
        }

        public Vector2[] controlPoints;
        public bool continuous;
        public int spanCount;
        private Vector2 tmp;
        private Vector2 tmp2;
        private Vector2 tmp3;

        public CatmullRomSpline()
        {
        }

        public CatmullRomSpline(Vector2[] controlPoints, bool continuous)
        {
            set(controlPoints, continuous);
        }

        public CatmullRomSpline set(Vector2[] controlPoints, bool continuous)
        {
            if (tmp == null)
            {
                tmp = new Vector2();
                tmp.X = controlPoints[0].X;
                tmp.Y = controlPoints[0].Y;
            }

            if (tmp2 == null)
            {
                tmp2 = new Vector2();
                tmp2.X = controlPoints[0].X;
                tmp2.Y = controlPoints[0].Y;
            }
            if (tmp3 == null)
            {
                tmp3 = new Vector2();
                tmp3.X = controlPoints[0].X;
                tmp3.Y = controlPoints[0].Y;
            }
            this.controlPoints = controlPoints;
            this.continuous = continuous;
            this.spanCount = continuous ? controlPoints.Length : controlPoints.Length - 3;
            return this;
        }

        public Vector2 valueAt(Vector2 _out, float t)
        {
            int n = spanCount;
            float u = t * n;
            int i = (t >= 1f) ? (n - 1) : (int)u;
            u -= i;
            return valueAt(_out, i, u);
        }

        /** @return The value of the spline at position u of the specified span */
        public Vector2 valueAt(Vector2 _out, int span, float u)
        {
            return calculate(_out, continuous ? span : (span + 1), u, controlPoints, continuous, tmp);
        }

        public float approxLength(int samples)
        {
            float tempLength = 0;
            for (int i = 0; i < samples; ++i)
            {
                tmp2 = tmp3;
                tmp3 = valueAt(tmp3, (i) / ((float)samples - 1));
                if (i > 0) tempLength += Vector2.Distance(tmp2, tmp3);
            }
            return tempLength;
        }
    }
}
