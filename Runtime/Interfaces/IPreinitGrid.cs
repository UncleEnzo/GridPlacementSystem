using System.Collections.Generic;

namespace Nevelson.GridPlacementSystem
{
    public interface IPreinitGrid
    {
        public void ReplacePreInitObjectsList(List<PreInitObject> preInitObjects);
        public void AddPreInitObjects(PreInitObject[] preInitObject);
    }
}