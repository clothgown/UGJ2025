using UnityEngine;
using UnityEngine.UI;

public class SliderController : MonoBehaviour
{
    [Header("Slider配置")]
    public Slider targetSlider; // 拖入你的Slider
    public float step = 0.1f; // 每次点击变化的步长
    public float minValue = 0f; // 最小值
    public float maxValue = 1f; // 最大值

    // 增加Slider值（右按钮）
    public void IncreaseSliderValue()
    {
        if (targetSlider != null)
        {
            targetSlider.value = Mathf.Clamp(targetSlider.value + step, minValue, maxValue);
        }
    }

    // 减少Slider值（左按钮）
    public void DecreaseSliderValue()
    {
        if (targetSlider != null)
        {
            targetSlider.value = Mathf.Clamp(targetSlider.value - step, minValue, maxValue);
        }
    }
}