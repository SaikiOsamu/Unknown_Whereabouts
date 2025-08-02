using UnityEngine;

[CreateAssetMenu(fileName = "AIController", menuName = "InputController/AIController")]

public class AIController : InputController
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override bool RetrieveJumpInput()
    {
        return true;
    }

    // Update is called once per frame
    public override float RetrieveMoveInput()
    {
        return 1f;
    }   
 
}
