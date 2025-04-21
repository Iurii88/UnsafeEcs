using Unity.Mathematics;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Additions.Components
{
    public partial struct Transform : IComponent
    {
        public float3 position;
        public quaternion rotation;
        public float3 scale;
        
        public Transform(float3 position)
        {
            this.position = position;
            this.rotation = quaternion.identity;
            this.scale = new float3(1f);
        }

        public Transform(float3 position, quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = new float3(1f);
        }

        public Transform(float3 position, float3 eulerAngles)
        {
            this.position = position;
            this.rotation = quaternion.EulerZXY(math.radians(eulerAngles));
            this.scale = new float3(1f);
        }

        public Transform(float3 position, quaternion rotation, float3 scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public Transform(float3 position, quaternion rotation, float uniformScale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = new float3(uniformScale);
        }

        public static Transform Default => new Transform(float3.zero, quaternion.identity, new float3(1f));

        public float3 Forward => math.mul(rotation, new float3(0, 0, 1));

        public void Rotate(float3 eulerAngles)
        {
            var radians = math.radians(eulerAngles);
            var deltaRotation = quaternion.EulerZXY(radians);
            rotation = math.mul(rotation, deltaRotation);
        }

        public void Translate(float3 translation)
        {
            position += math.mul(rotation, translation);
        }
    }
}