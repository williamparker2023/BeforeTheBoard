using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(NetworkTransform))]
public class BenPlayerTest : NetworkBehaviour
{

    // ============== Physics ==============
    Rigidbody2D rb = null;
    [SerializeField] float SPEED = 0.0f;
    [SerializeField] Transform bulletTransform;
    public GameObject bullet;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {        
        if (IsOwner)
        {
            rb = GetComponent<Rigidbody2D>();
            GetComponent<SpriteRenderer>().color = Color.green;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        
        PlayerMovement();
    }

    void PlayerMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector2 movement = new Vector2(horizontalInput * SPEED, verticalInput * SPEED);
        rb.linearVelocity = movement;

        if(Input.GetKeyDown(KeyCode.Space))
        {
            // Instantiate(bullet, bulletTransform.position, Quaternion.identity);
            var instance = Instantiate(bullet, bulletTransform.position, Quaternion.identity);
            var instanceNetworkObject = instance.GetComponent<NetworkObject>();
            instanceNetworkObject.Spawn();
        }
    }
}
