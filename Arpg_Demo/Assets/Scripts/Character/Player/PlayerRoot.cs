using System;
using LLKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerRoot : SingletonMonoBase<PlayerRoot>
{
    [LabelText("角色控制器"), SerializeField] private CharacterController _playerController;
    public CharacterController CharacterController => _playerController;
}