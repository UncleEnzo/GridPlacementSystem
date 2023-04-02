using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nevelson.GridPlacementSystem
{
    public class Test_GridPlacementSystem
    {
        // Set up units for this repo
        // THE INIT IS BROKEN, ISSUE WITH ASSOCIATING PREFABBED STUFF IN PACKAGE

        [UnityTest]
        public IEnumerator Test_BuildActionOnlyWorksInBuildMode()
        {
            GameObject gridSystem = new GameObject("TestObject");
            var gbs = gridSystem.AddComponent<GridBuildingSystem>();
            gbs.Test_DefaultInit();
            yield return null;
            bool ok = gbs.DisplayGrid(true);
            Assert.True(ok);
            GameObject.Destroy(gridSystem);
        }


        // Verify can’t change mode to same mode
        // Verify switching from move to build or demolish unselects the object
        // Verify that the onUpdate reports all changes
        // Verify can’t rotate if unrotatable obj
        // Verify can’t move if unmovable obj
        // Verify can’t destroy undestroyable obj
        // Verify grid position check function
        // Verify when moving and object and midmove you hide grid (the object gets put back first

    }
}