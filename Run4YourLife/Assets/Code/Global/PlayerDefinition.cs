﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Run4YourLife.GameInput;

namespace Run4YourLife.Player
{
    public enum CharacterType
    {
        Red,
        Green,
        Blue,
        Orange
    }

    public class PlayerDefinition
    {
        public bool IsBoss { get; set; }
        public Controller Controller { get; set; }
        public CharacterType characterType;
    }
}