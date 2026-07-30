using Shura.Camera;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerOwnerSetup : NetworkBehaviour
{
    private PlayerInput playerInput;
    private PlayerAutoAttack autoAttack;
    private PlayerExperience playerExperience;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        autoAttack = GetComponent<PlayerAutoAttack>();
        playerExperience = GetComponent<PlayerExperience>();

        SetLocalGameplayEnabled(false);
    }

    public override void OnNetworkSpawn()
    {
        SetLocalGameplayEnabled(IsOwner);

        if (!IsOwner)
        {
            return;
        }

        CameraFollow cameraFollow = FindFirstObjectByType<CameraFollow>();

        if (cameraFollow == null && UnityEngine.Camera.main != null)
        {
            cameraFollow = UnityEngine.Camera.main.gameObject.AddComponent<CameraFollow>();
        }

        cameraFollow?.SetTarget(transform);
    }

    public override void OnNetworkDespawn()
    {
        SetLocalGameplayEnabled(false);
    }

    private void SetLocalGameplayEnabled(bool isEnabled)
    {
        if (playerInput != null)
        {
            playerInput.enabled = isEnabled;
        }

        if (autoAttack != null)
        {
            autoAttack.enabled = isEnabled;
        }

        if (playerExperience != null)
        {
            playerExperience.enabled = isEnabled;
        }
    }
}
