﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Run4YourLife.Player;
using Run4YourLife.GameInput;
using System;

namespace Run4YourLife.CharacterSelection
{
    public class CharacterSelectionManager : MonoBehaviour
    {
        PlayerManager playerManager;

        void Awake()
        {
            playerManager = Component.FindObjectOfType<PlayerManager>();
            Debug.Assert(playerManager != null);

            ControllerDetector controllerDetector = GetComponent<ControllerDetector>();
            Debug.Assert(controllerDetector != null);
            controllerDetector.OnControllerDetected += OnControllerDetected;
        }

        public void OnControllerDetected(Controller controller)
        {
            CreatePlayerForController(controller);
        }

        private void CreatePlayerForController(Controller controller)
        {
            PlayerDefinition playerDefinition = new PlayerDefinition();
            playerDefinition.Controller = controller;
            if (playerManager.GetPlayers().Count == 1)
            {
                playerManager.SetPlayerAsBoss(playerDefinition);
            }
            playerDefinition.CharacterType = GetFirstAviablePlayerCharacterType();

            playerManager.AddPlayer(playerDefinition);
        }

        private CharacterType GetFirstAviablePlayerCharacterType()
        {
            CharacterType ret = CharacterType.Red;
            foreach (CharacterType characterType in Enum.GetValues(typeof(CharacterType)))
            {
                if (!PlayerHasCharacterType(characterType))
                {
                    ret = characterType;
                    break;
                }
            }
            return ret;
        }

        private bool PlayerHasCharacterType(CharacterType characterType)
        {
            foreach(PlayerDefinition player in playerManager.GetPlayers())
            {
                if (player.CharacterType.Equals(characterType))
                {
                    return true;
                }
            }
            return false;
        }
    }
}