using Shura.Camera;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerOwnerSetup : NetworkBehaviour
{
    private PlayerInput playerInput;
    private PlayerAutoAttack autoAttack;
    private PlayerAimDirection aimDirection;
    private DirectionalAutoAttack directionalAutoAttack;
    private AutoSkillCaster autoSkillCaster;
    private PlayerExperience playerExperience;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        autoAttack = GetComponent<PlayerAutoAttack>();
        aimDirection = GetComponent<PlayerAimDirection>();
        directionalAutoAttack = GetComponent<DirectionalAutoAttack>();
        autoSkillCaster = GetComponent<AutoSkillCaster>();
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
            autoAttack.enabled = isEnabled && directionalAutoAttack == null;
        }

        if (aimDirection != null)
        {
            aimDirection.enabled = isEnabled;
        }

        if (directionalAutoAttack != null)
        {
            directionalAutoAttack.enabled = isEnabled;
        }

        if (autoSkillCaster != null)
        {
            autoSkillCaster.enabled = isEnabled;
        }

        if (playerExperience != null)
        {
            playerExperience.enabled = isEnabled;
        }
    }
}
