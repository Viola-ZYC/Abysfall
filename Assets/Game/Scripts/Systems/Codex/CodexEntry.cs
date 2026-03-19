using System;
using UnityEngine;

namespace EndlessRunner
{
    [Serializable]
    public class CodexEntry
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public string unlockHint;
        public int spawnScore;
        public string icon;
        public string note;
        public string abilityId;
        public bool isPassive;
    }
}
