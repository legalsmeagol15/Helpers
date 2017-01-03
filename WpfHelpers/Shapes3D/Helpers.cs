using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WpfHelpers.Shapes3D
{
    internal static class Helpers
    {
        public static Vector3D GetNormal(Vector3D leftHand,  Vector3D rightHand)
        {
            return Vector3D.CrossProduct(leftHand, rightHand);
        }

        /// <summary>
        /// Returns a polygon, centered at the given location, with the given radius and the count of vertices (sides), in terms of a three-
        /// dimensional shape.  The polygon's vertices will rotate around the given axis of rotation.
        /// </summary>
        /// <param name="center">The center of the polygon.</param>
        /// <param name="axis">The rotation axis of the polygon, around the given center.</param>
        /// <param name="radius">The radius between the center and the vertices of the polygon.</param>
        /// <param name="count">The count of vertices (or sides) of the polygon.</param>
        /// <param name="startAngle">Optional.  The starting angle of the polygon.  If the rotation axis is the z-axis, and startAngle==0.0, then 
        /// the first point will be placed on the x-axis.</param>
        /// <returns>Returns an array of Point3D structs describing the polyon.</returns>
        public static Point3D[] GetPolygon(Point3D center, Vector3D axis, double radius, int count, double startAngle = 0.0)
        {
            Point3D[] result = GetPolygon(center, radius, count, startAngle);

            //TODO:  Helpers.GetPolygon - matrix calculation can be optimized.
            Vector3D upVector = new Vector3D(0, 1, 0);
            Vector3D rotationAxis = Vector3D.CrossProduct(axis, upVector);  //The axis of rotation.
            double rotationRadians = System.Math.Acos(Vector3D.DotProduct(axis, upVector));  //The amount of rotation.
            AxisAngleRotation3D rotator = new AxisAngleRotation3D(rotationAxis, (rotationRadians * 180) / System.Math.PI);
            RotateTransform3D transformer = new RotateTransform3D(rotator, center);
            
            //Return the transformed result.
            transformer.Transform(result);
            return result;
        }

        /// <summary>
        /// Returns a polygon, centered at the given location, with the given radius and the count of vertices (sides), in terms of a three-
        /// dimensional shape.  The polygon will be parallel to the xz-plane, with the normal in the y-axis direction.
        /// </summary>
        /// <param name="center">The center of the polygon.</param>        
        /// <param name="radius">The radius between the center and the vertices of the polygon.</param>
        /// <param name="count">The count of vertices (or sides) of the polygon.</param>
        /// <param name="startAngle">Optional.  The starting angle of the polygon.
        /// <returns>Returns an array of Point3D structs describing the polyon.</returns>
        public static Point3D[] GetPolygon(Point3D center, double radius, int count, double startAngle = 0.0)
        {
            Point3D[] result = new Point3D[count];
            double arcStep = (System.Math.PI * 2) / count;
            for (int i = 0; i < count; i++)
                result[i] = new Point3D(center.X + (radius * System.Math.Cos(startAngle)), center.Y + (radius * System.Math.Sin(startAngle)), center.Z);
            return result;
        }


        
    }
}
