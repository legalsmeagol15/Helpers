using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WpfHelpers.Shapes3D
{
    class Globe3D
    {
        protected Globe3D()
        {

        }


        public Globe3D FromLatitudeLongitude(int latitudes, int longitudes)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="latitudes">The number of latitude lines, excluding the north or south pole.</param>
        /// <param name="longitudes">The number of longitude lines.</param>
        /// <param name="mapTexture"></param>
        /// <returns></returns>
        public static MeshGeometry3D MeshFromLatLong(int latitudes, int longitudes, double radius, bool mapTexture = false)
        {
            MeshGeometry3D result = new MeshGeometry3D();

            //First, specify the latitudes.
            double[] lats = new double[latitudes+1];
            double latArcStep = System.Math.PI / (latitudes + 1), latArc = 0.0;
            for (int lat = 0; lat <= latitudes; lat++)
            {                
                lats[lat] =  radius * System.Math.Cos(latArc);
                latArc += latArcStep;
            }

            //Second, the north cap.
            
           

            for (int lat = 0; lat < latitudes; lat++)
            {

            }
        }
    }
}
