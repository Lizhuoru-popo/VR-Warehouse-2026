using UnityEngine;

public class CharacterControllerMove : MonoBehaviour
{
    public CharacterController cc;
    public float moveSpeed = 3f;

    void Update()
    {
        if (cc == null)
        {
            cc = GetComponent<CharacterController>();
            if (cc == null) return;
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 dir = transform.right * h + transform.forward * v;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.01f)
        {
            cc.Move(dir.normalized * moveSpeed * Time.deltaTime);
        }
    }
}
