using System;
using UnityEngine;
using GameSaveManagement.Service;

namespace GameSaveManagement.Controller
{
    public class GameSaveController : MonoBehaviour
    {
        private IGameSaveService _gameSaveService;

        private void Awake()
        {
            _gameSaveService = new GameSaveService();
        }

        public void SavePlayerPosition(Vector3 position)
        {
            _gameSaveService.SavePlayerPosition(position);
        }

        public Vector3 LoadPlayerPosition()
        {
            return _gameSaveService.LoadPlayerPosition();
        }
    }
}