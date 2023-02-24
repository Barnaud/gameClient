using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject objectToFollow;
    public int followMaxAngle = 10;
    
    private float altitudeToObject;
    private Vector3 verticalMovementVector;
    private Vector3 horizontalMovementVector;

    public float screenRatio = 16f / 9f;

    // Start is called before the first frame update
    void Start()
    {
        altitudeToObject = transform.position.y - objectToFollow.transform.position.y;
        horizontalMovementVector = transform.right;

        if(transform.rotation.x == 90)
        {
            verticalMovementVector = transform.up;
        }
        else if(transform.rotation.x == 0)
        {
            verticalMovementVector = transform.forward;
        }
        else
        {
            verticalMovementVector = Vector3.Normalize(Vector3.ProjectOnPlane(transform.up, Vector3.up));
        }


    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(transform.position.x, altitudeToObject + objectToFollow.transform.position.y, transform.position.z);
        moveToFollow();

    }

    void moveToFollow()
    {
        Vector3 vectToObject = objectToFollow.transform.position - transform.position;
        Vector3 vectToObjectHorizontal = Vector3.ProjectOnPlane(vectToObject, transform.up);
        Vector3 vectToObjectVertical = Vector3.ProjectOnPlane(vectToObject, transform.right);
        /*        Quaternion quaterionToObject = Quaternion.FromToRotation(vectToObject, transform.forward);
                *//*        float horizontalAngle = Vector3.SignedAngle(transform.forward, vectToObject, transform.up);
                        float verticalAngle = Vector3.SignedAngle(transform.forward, vectToObject, transform.right);
                        Debug.Log($"Horizontal: {horizontalAngle}, Vertical: {verticalAngle}");
                *//*
                Debug.Log(quaterionToObject);*/
        //Vector3 quaterionToObjectEuler = quaterionToObject.eulerAngles;
        float horizontalAngle = Vector3.SignedAngle(vectToObjectHorizontal, transform.forward, -transform.up);
        float verticalAngle = Vector3.SignedAngle(vectToObjectVertical, transform.forward, transform.right);
        //Debug.Log($"Horizontal: {horizontalAngle}, Vertical: {verticalAngle}");
        if (Mathf.Abs(horizontalAngle) > followMaxAngle)
        {
            float horizontalMovementSize = (Mathf.Asin(horizontalAngle * Mathf.Deg2Rad) - Mathf.Asin(Mathf.Sign(horizontalAngle) * followMaxAngle * Mathf.Deg2Rad)) * transform.position.y;
            Vector3 horizontalTranslateVector = horizontalMovementSize *  horizontalMovementVector;
            transform.Translate(horizontalTranslateVector, Space.World);
        }
        if(Mathf.Abs(verticalAngle) > followMaxAngle)
        {
            float verticalMovementSize = (Mathf.Asin(verticalAngle * Mathf.Deg2Rad) - Mathf.Asin(Mathf.Sign(verticalAngle) * followMaxAngle * Mathf.Deg2Rad)) * transform.position.y;
            Vector3 verticalTranslateVector = verticalMovementSize * verticalMovementVector;
            transform.Translate(verticalTranslateVector, Space.World);
            //TODO: Faire en sorte que le mouvement de camera soit dans le plan X/Z  du referentiel world
            //Vector3 verticalTranslateVector = new Vector3((Mathf.Asin(verticalAngle * Mathf.Deg2Rad) - Mathf.Asin(Mathf.Sign(verticalAngle) * followMaxAngle * Mathf.Deg2Rad)) * transform.position.y), 0, 0);
        }
    }

}
