using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Aquarium
{
    public abstract class MovementRules : ScriptableObject
    {
        public float SwimSpeed;
        public abstract Vector3 FindLocation(Vector3? current, float fishSize);
    }

    public class CylinderMovement : MovementRules
    {
        public float Radius;
        public float Bottom;
        public float Top;
        public override Vector3 FindLocation(Vector3? current, float fishSize)
        {
            var r = Random.Range(0, 360);
            var d = Mathf.Sin(Random.Range(0, Mathf.PI / 2)) * (Radius - fishSize);
            return new Vector3(Mathf.Sin(r) * d, Random.Range(Bottom + fishSize, Top - fishSize), Mathf.Cos(r) * d);
        }
    }

    public class SimpleBowlMovement : MovementRules
    {
        public float Radius;
        public float Height;
        public override Vector3 FindLocation(Vector3? current, float fishSize)
        {
            if (current == null)
                return new Vector3(Radius - fishSize, Height, 0);
            return (Quaternion.Euler(0, 15, 0) * current.Value.XZOnly().normalized * (Radius - fishSize)) + new Vector3(0,Height,0);
        }
    }

    public class BoxMovement : MovementRules
    {
        public Vector3 Min;
        public Vector3 Max;
        public override Vector3 FindLocation(Vector3? current, float fishSize) => new Vector3(Random.Range(Min.x + fishSize, Max.x - fishSize), Random.Range(Min.y + fishSize, Max.y - fishSize), Random.Range(Min.z + fishSize, Max.z - fishSize));
    }
}
