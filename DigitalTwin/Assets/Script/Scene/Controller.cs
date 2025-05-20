using UnityEngine;

public class FirstPersonCamera : MonoBehaviour
{
    public float speed = 5f; // Vitesse de déplacement
    public float mouseSensitivity = 2f; // Sensibilité de la souris

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Update()
    {
        // Vérifier si Shift est enfoncé
        if (Input.GetKey(KeyCode.LeftShift))
        {
            // Verrouiller la souris et activer la rotation
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f); // Limite la rotation verticale

            rotationY += mouseX; // Rotation horizontale

            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
        }
        else
        {
            // Débloquer la souris quand Shift N'EST PAS enfoncé
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        

        // Ajustement de la vitesse selon la hauteur
        float height = transform.position.y;

        if (height >= 200 && height < 1000)
            speed = 240;
        else if (height >= 1000 && height < 1500)
            speed = 360;
        else if (height >= 1500)
            speed = 500;
        else if (height >= 50 && height < 200)
            speed = 160;
        else if (height >= 0 && height < 50)
            speed = 80;
        else if (height < 0 && height >= -200)
            speed = 160;
        else if (height < -200 && height >= -350)
            speed = 100;
        else
            speed = 40;

        // Déplacement en fonction de la direction de la caméra
        float moveX = Input.GetAxis("Horizontal"); // Q/D ou A/D
        float moveZ = Input.GetAxis("Vertical");   // Z/S ou W/S

        Vector3 move = transform.forward * moveZ + transform.right * moveX;
        transform.position += move.normalized * speed * Time.deltaTime;
    }
}
