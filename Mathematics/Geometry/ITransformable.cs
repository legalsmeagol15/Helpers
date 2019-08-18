using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics
{
    public interface ITransformable<T, TMatrix>
    {
        T GetTransformed(TMatrix matrix);
    }
}
