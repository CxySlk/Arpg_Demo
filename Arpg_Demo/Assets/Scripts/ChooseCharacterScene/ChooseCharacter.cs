using LLKFrame;
using UnityEngine;

public class ChooseCharacter : SingletonMonoBase<ChooseCharacter>
{
    // 角色模型
    private Transform characterModel;

    public void CreateCharacter(GameObject prefab,AnimationClip clip)
    {
        // 删除旧的模型，创建新的
        if (characterModel != null) Destroy(characterModel.gameObject);
        characterModel = Instantiate(prefab, transform).transform;
        characterModel.position = Vector3.zero;
        // 播放显示动画
        AnimationController animation = characterModel.GetComponent<AnimationController>();
        animation.Init();
        animation.PlaySingleAnimation(clip);
    }
    /// <summary>控制角色模型绕Y轴旋转</summary>
    public void RotateModel(float yAngle) => characterModel?.Rotate(0, yAngle, 0);
}
