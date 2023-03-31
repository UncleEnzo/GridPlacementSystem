using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nevelson.GridPlacementSystem
{
    public class Test_GridPlacementSystem
    {
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

        //todo add units for everything in TEST_GRID
    }
}