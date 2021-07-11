using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public float Speed = 4f;
    public float JumpHeight = 1f;
    public float _sensivity = 3f;
    public Transform Cam;

    private CharacterController _controller;
    private Vector3 _velocity;
    private Vector2 _mouse;
    private float _gravity = -9.81f;
    private bool _isGrounded;
    private float _rotationX = 0f;

    void Start()
    {
        _controller = gameObject.AddComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * _sensivity;
        float mouseY = Input.GetAxis("Mouse Y") * _sensivity;

        _rotationX -= mouseY;
        _rotationX = Mathf.Clamp(_rotationX, -90f, 90f);

        _isGrounded = _controller.isGrounded;

        if (_isGrounded && _velocity.y < 0)
            _velocity.y = 0f;

        Cam.localRotation = Quaternion.Euler(_rotationX, 0, 0);
        transform.Rotate(Vector3.up * mouseX);

        Vector3 move = transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical");
        _controller.Move(move * Time.deltaTime * Speed);

        if (Input.GetKey(KeyCode.Space) && _isGrounded)
            _velocity.y += Mathf.Sqrt(JumpHeight * -3f * _gravity);

        _velocity.y += _gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }
}
