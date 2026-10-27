using UnityEngine;

public partial class PlayerController
{
    #region 移动、旋转

    public void Rotate(Vector2 moveInput)
    {
        Vector3 input = new(moveInput.x, 0, moveInput.y);
        Vector3 target = Quaternion.Euler(0, Camera.main!.transform.eulerAngles.y, 0) * input;

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target), 10 * Time.deltaTime);
    }

    public override void Move(Vector3 deltaPos)
    {
        if (!characterController.enabled) return;
        characterController.Move(deltaPos);
    }

    public void Move(Vector2 moveInput, float moveSpeed)
    {
        Vector3 moveDir = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        moveDir = Quaternion.Euler(0, Camera.main!.transform.eulerAngles.y, 0) * moveDir;
        characterController.Move(moveDir * (moveSpeed * Time.deltaTime));
    }

    #endregion
}
