using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;

public class Froguelike_CharacterController : MonoBehaviour
{
    [Header("Player id")]
    public int playerID;

    [Header("References")]
    public Transform weaponsParent;
    public Transform weaponStartPoint;
    public Transform healthBar;

    [Header("Character data")]
    public float landSpeed;
    public float swimSpeed;
    [Space]
    public float currentHealth = 100;
    public float maxHealth = 100;
    public float healthRecovery = 0.01f;
    [Space]
    [Range(0,0.5f)]
    public float armorBoost = 0;
    [Range(0, 1)]
    public float experienceBoost = 0;
    public float revivals = 0;
    [Space]
    [Range(0, 1)]
    public float attackCooldownBoost = 0;
    [Range(0, 1)]
    public float attackDamageBoost = 0;
    [Range(0, 1)]
    public float attackMaxFliesBoost = 0;
    [Range(0, 1)]
    public float attackRangeBoost = 0;
    [Range(0, 1)]
    public float attackSpeedBoost = 0;

    [Header("Settings - controls")]
    public string horizontalInputName;
    public string verticalInputName;

    private Player rewiredPlayer;

    public float HorizontalInput { get; private set; }
    public float VerticalInput { get; private set; }

    [Header("Animator")]
    public Animator animator;

    private int animatorCharacterValue;

    private Rigidbody2D playerRigidbody;

    private bool isOnLand;

    private float orientationAngle;

    private float invincibilityTime;

    // Start is called before the first frame update
    void Start()
    {
        isOnLand = true;
        invincibilityTime = 0;
        rewiredPlayer = ReInput.players.GetPlayer(playerID);
        playerRigidbody = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Froguelike_GameManager.instance.isGameRunning)
        {
            foreach (Transform weaponTransform in weaponsParent)
            {
                weaponTransform.GetComponent<Froguelike_TongueBehaviour>().TryAttack();
            }
            ChangeHealth(healthRecovery);
        }
    }

    private void SetAnimatorCharacterValue(int value)
    {
        animatorCharacterValue = value;
        animator.SetInteger("character", animatorCharacterValue);
    }

    public void ForceGhost(bool isGhost)
    {
        animator.SetInteger("character", isGhost ? 2 : animatorCharacterValue);
    }

    public void InitializeCharacter(Froguelike_PlayableCharacterInfo characterInfo)
    {
        SetAnimatorCharacterValue(characterInfo.characterAnimatorValue);

        healthRecovery = characterInfo.startingHealthRecovery;
        landSpeed = characterInfo.startingLandSpeed;
        swimSpeed = characterInfo.startingSwimSpeed;
        maxHealth = characterInfo.startingMaxHealth;

        armorBoost = characterInfo.startingArmor;
        revivals = characterInfo.startingRevivals;

        currentHealth = maxHealth;
    }

    public void ResolvePickedItemLevel(Froguelike_ItemLevel itemLevelData)
    {
        Froguelike_FliesManager.instance.curse += itemLevelData.curseBoost;
        
        // character stats
        armorBoost += itemLevelData.armorBoost;
        experienceBoost += itemLevelData.experienceBoost;
        healthRecovery += itemLevelData.healthRecoveryBoost;
        maxHealth += itemLevelData.maxHealthBoost;
        revivals += itemLevelData.revivalBoost;

        landSpeed += itemLevelData.walkSpeedBoost;
        swimSpeed += itemLevelData.swimSpeedBoost;

        // attack stuff
        attackCooldownBoost += itemLevelData.attackCooldownBoost;
        attackDamageBoost += itemLevelData.attackDamageBoost;
        attackMaxFliesBoost += itemLevelData.attackMaxFliesBoost;
        attackRangeBoost += itemLevelData.attackRangeBoost;
        attackSpeedBoost += itemLevelData.attackSpeedBoost;
       
        if (itemLevelData.recoverHealth > 0)
        {
            currentHealth += Mathf.Clamp(currentHealth + itemLevelData.recoverHealth, 0, maxHealth);
        }
    }


    public void Respawn()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
        invincibilityTime = 1;
    }

    private void FixedUpdate()
    {
        UpdateHorizontalInput();
        UpdateVerticalInput();

        if (invincibilityTime > 0)
        {
            invincibilityTime -= Time.fixedDeltaTime;
        }

        float moveSpeed = isOnLand ? landSpeed : swimSpeed;
        Vector2 moveInput = (((HorizontalInput * Vector2.right).normalized + (VerticalInput * Vector2.up).normalized)).normalized * moveSpeed;

        if (!moveInput.Equals(Vector2.zero))
        {
            orientationAngle = 90 + 90 * Mathf.RoundToInt((Vector2.SignedAngle(moveInput, Vector2.right)) / 90);
            transform.localRotation = Quaternion.Euler(0, 0, -orientationAngle);
        }

        playerRigidbody.velocity = moveInput;

        foreach (Transform weaponTransform in weaponsParent)
        {
            weaponTransform.GetComponent<Froguelike_TongueBehaviour>().SetTonguePosition(weaponStartPoint);
        }
    }

    #region Update Inputs

    private void UpdateHorizontalInput()
    {
        HorizontalInput = rewiredPlayer.GetAxis(horizontalInputName);
    }

    private void UpdateVerticalInput()
    {
        VerticalInput = rewiredPlayer.GetAxis(verticalInputName);
    }

    #endregion

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Water"))
        {
            isOnLand = false;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Water"))
        {
            isOnLand = true;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Fly") && Froguelike_GameManager.instance.isGameRunning && invincibilityTime <= 0)
        {
            float damage = Froguelike_FliesManager.instance.GetEnemyDataFromName(collision.gameObject.name).damage * Froguelike_FliesManager.instance.enemyDamageFactor;
            damage = damage * (1-armorBoost);
            ChangeHealth(-damage);
        }
    }

    private void ChangeHealth(float change)
    {
        currentHealth += change;
        if (currentHealth >= maxHealth)
        {
            currentHealth = maxHealth;
        }
        if (currentHealth <= 0)
        {
            // game over
            Froguelike_GameManager.instance.TriggerGameOver();
        }
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        float healthRatio = currentHealth / maxHealth;
        if (healthRatio < 0)
        {
            healthRatio = 0;
        }
        healthBar.localScale = healthRatio * Vector3.right + healthBar.localScale.y * Vector3.up + healthBar.localScale.z * Vector3.forward;
    }
}
