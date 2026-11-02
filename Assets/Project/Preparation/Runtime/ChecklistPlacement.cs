using UnityEngine;

namespace GroupProject.Preparation
{
    public static class ChecklistPlacement
    {
        // Keep the whole card, including its toggle tab, inside the shared binocular view.
        public static void Place(Transform panel, Transform head, float width, float height, float tab)
        {
            if(panel.parent!=head)panel.SetParent(head,false);
            var camera = head.GetComponent<Camera>();
            const float distance = 1.2f;
            float halfX = distance * .62f, halfY = distance * .46f;
            if (camera)
            {
                var projection = camera.projectionMatrix;
                halfX = Mathf.Min(halfX, distance / Mathf.Abs(projection.m00) * .88f);
                halfY = Mathf.Min(halfY, distance / Mathf.Abs(projection.m11) * .88f);
                if (camera.stereoEnabled)
                    for (int eye = 0; eye < 2; eye++)
                    {
                        var p = camera.GetStereoProjectionMatrix((Camera.StereoscopicEye)eye);
                        halfX = Mathf.Min(halfX, distance * (1f - Mathf.Abs(p.m02)) / Mathf.Abs(p.m00) * .88f);
                        halfY = Mathf.Min(halfY, distance * (1f - Mathf.Abs(p.m12)) / Mathf.Abs(p.m11) * .88f);
                    }
            }
            float scale = Mathf.Min(.001f, halfX * 1.25f / width, halfY * 1.65f / (height + tab));
            panel.localScale = Vector3.one * scale;
            panel.localPosition=new Vector3(-halfX + Mathf.Min(.16f,halfX*.35f), halfY - tab * scale - .05f, distance);
            panel.localRotation=Quaternion.identity;
        }
    }
}
