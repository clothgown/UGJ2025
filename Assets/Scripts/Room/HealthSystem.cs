using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    [Header("UI Elements")]
    public Image healthBar;   // 血量条
    public Image healthBarinUI;
    public string healthper;
    public TMP_Text healthText;   // 修改：使用 TextMeshPro 组件显示血量文本
    public Image shieldBar;   // 护盾条（可选）

    private float maxHealth;
    private float currentHealth; // 记录当前血量
    private float maxShield;
    private bool isDead = false;

    [Header("Smoothing")]
    public float smoothTime = 0.2f;

    // 死亡事件，其他脚本可以订阅
    public System.Action<UnitController> OnDeath;

    public bool IsDead => isDead;

    // 设置最大血量
    public void SetMaxHealth(float health)
    {
        maxHealth = health;
        currentHealth = health; // 初始化当前血量
        StartCoroutine(SmoothHealthChange(healthBar, health, maxHealth));
        StartCoroutine(SmoothHealthChange(healthBarinUI, health, maxHealth));
        UpdateHealthText(); // 更新血量文本
    }

    // 设置当前血量
    public void SetHealth(float health)
    {
        currentHealth = health;
        StartCoroutine(SmoothHealthChange(healthBar, health, maxHealth));
        StartCoroutine(SmoothHealthChange(healthBarinUI, health, maxHealth));
        UpdateHealthText(); // 更新血量文本
        // 检查死亡
        if (health <= 0 && !isDead)
        {
            Die();
        }
    }

    // 设置最大护盾
    public void SetMaxShield(float shield)
    {
        maxShield = shield;
    }

    // 设置当前护盾
    public void SetShield(float shieldValue)
    {
        if (shieldBar != null)
        {
            StartCoroutine(SmoothHealthChange(shieldBar, shieldValue, maxShield));
        }
    }
    public string GetHealthString()
    {
        return $"{Mathf.RoundToInt(currentHealth)}/{Mathf.RoundToInt(maxHealth)}";
    }

    // 更新血量文本显示
    private void UpdateHealthText()
    {
        if (healthText != null)
        {
            healthText.text = GetHealthString();
        }

        // 如果你还想在其他地方使用这个字符串，可以赋值给healthper
        healthper = GetHealthString();
    }

    // 死亡方法
    private void Die()
    {
        isDead = true;
        Debug.Log($"{gameObject.name} 已死亡");

        // 触发死亡事件，传递UnitController引用
        UnitController unitController = GetComponent<UnitController>();
        if (unitController != null)
        {
            unitController.SetDeadAppearance();
        }
        OnDeath?.Invoke(unitController);

        // 可以在这里添加死亡特效、动画等
        StartCoroutine(DeathRoutine());
    }

    // 死亡协程
    private IEnumerator DeathRoutine()
    {
        // 播放死亡动画或特效
        // 例如：GetComponent<Animator>().SetTrigger("Die");

        // 等待一段时间后隐藏或销毁对象
        yield return new WaitForSeconds(2f);

        // 可以选择隐藏对象而不是立即销毁
        gameObject.SetActive(false);
    }

    // 复活方法（如果需要）
    public void Revive(float health)
    {
        isDead = false;
        SetHealth(health);
        gameObject.SetActive(true);
    }

    // 平滑变化协程
    private IEnumerator SmoothHealthChange(Image bar, float targetValue, float maxValue)
    {
        float currentValue = bar.fillAmount * maxValue;
        float elapsedTime = 0f;

        while (elapsedTime < smoothTime)
        {
            elapsedTime += Time.deltaTime;
            bar.fillAmount = Mathf.Lerp(currentValue, targetValue, elapsedTime / smoothTime) / maxValue;
            yield return null;
        }

        // 精确设置最终值
        bar.fillAmount = targetValue / maxValue;
    }
}