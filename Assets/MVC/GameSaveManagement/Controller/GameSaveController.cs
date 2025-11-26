using System;
using UnityEngine;
using GameSaveManagement.Model;

namespace GameSaveManagement.Controller
{
    public class GameSaveController : MonoBehaviour
    {
        private GameSaveModel _gameSaveModel;

        private void Awake()
        {
            _gameSaveModel = new GameSaveModel();
        }

        public void SavePlayerPosition(Vector3 position)
        {
            _gameSaveModel.SavePosition(position);
        }

        public Vector3 LoadPlayerPosition()
        {
            return _gameSaveModel.LoadPosition();
        }
    }
}