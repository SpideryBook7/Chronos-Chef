using UnityEngine;
namespace ChronosChef
{
    [ExecuteAlways]
    public sealed class KitchenStageScale : MonoBehaviour
    {
        [SerializeField] RectTransform stage;
        public void Configure(RectTransform target) { stage = target; }
        void LateUpdate()
        {
            if (stage) stage.localScale = Vector3.one * (((RectTransform)transform).rect.width / 1600f);
        }
    }
}
