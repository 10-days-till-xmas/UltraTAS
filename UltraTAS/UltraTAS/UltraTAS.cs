using System.Collections;
using UnityEngine;


namespace _UltraTAS
{
    internal static class UltraTAS
    {
        internal static string[]? TASList;

        internal static bool wasTSUsedThisScene = false;

        internal static List<string> TempTAS = [];

        internal static IEnumerator AdvanceFrame()
        {
            Time.timeScale = 1f;
            yield return new WaitForEndOfFrame();
            Time.timeScale = 0f;
            yield break;
        }

        internal static string MakeNewSave(string TASName)
        {
            return UltraTasConfig.FileSavePath + TASName;
        }


    }
}