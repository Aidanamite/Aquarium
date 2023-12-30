using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Aquarium
{
    public class FishAnimator : MonoBehaviour
    {
        public float Size = 0.1f;
        [NonSerialized]
        public MovementRules Movement;
        public Transform[] Bones = new Transform[Main.boneCount + 1];
        List<Bend> Bends = new List<Bend>();
        class Bend
        {
            public Quaternion angle;
            public float traveled;
            public Bend(Quaternion Angle, float Traveled)
            {
                angle = Angle;
                traveled = Traveled;
            }
        }
        public float StepLength;
        public float HeadLength = 0.15f;
        public float BendLength = 0.5f;
        public bool WiggleX = true;
        public bool WiggleY = false;
        public float WiggleVolume = 3;
        public float WiggleSpeed = 2;
        public float WiggleAmplitude = 30;
        float time;
        Vector3 target;
        public void OnSpawn(MovementRules movement)
        {
            Movement = movement;
            transform.localPosition = Movement.FindLocation(null, Size);
            PickTarget(false);
        }
        void PickTarget(bool modifyAnimations = true)
        {
            target = Movement.FindLocation(transform.localPosition, Size);
            Quaternion prev = default;
            if (modifyAnimations)
                prev = transform.rotation;
            transform.localRotation = Quaternion.LookRotation(target - transform.localPosition);
            if (modifyAnimations)
                Bends.Add(new Bend(transform.InverseTransformRotation(prev), -StepLength * ((BendLength) * Bones.Length)));
        }
        void Update()
        {
            if (Movement)
            {
                var distanceToMove = Time.deltaTime * Movement.SwimSpeed;
                while (distanceToMove > 0)
                {
                    transform.localPosition = transform.localPosition.MoveTowards(target, distanceToMove, out var moved);
                    foreach (var b in Bends)
                        b.traveled += moved;
                    if (distanceToMove > moved)
                        PickTarget();
                    distanceToMove -= moved;
                }
            }
            var bends = new Quaternion[Bones.Length + (int)(Bones.Length * BendLength)];
            for (int i = 0; i < bends.Length; i++)
                bends[i] = Quaternion.identity;
            if (Movement)
            {
                for (var j = 0; j < Bends.Count; j++)
                {
                    var t = Bends[j];
                    var p = t.traveled / StepLength;
                    var ind = Mathf.FloorToInt(p) + (int)(HeadLength * Bones.Length);
                    if (ind >= Bones.Length)
                    {
                        Bends.RemoveAt(j);
                        j--;
                        continue;
                    }
                    float remain = Bones.Length * BendLength;
                    for (int i = ind + (int)(Bones.Length * BendLength); i >= HeadLength * Bones.Length && i > ind; i--)
                    {
                        var amount = 0f;
                        if (i == ind + (int)(Bones.Length * BendLength))
                            amount = p % 1 + (p < 0 ? 1 : 0);
                        else
                            amount = Math.Min(remain, 1);
                        bends[i] = Quaternion.Lerp(Quaternion.identity, t.angle, amount / (Bones.Length * BendLength)) * bends[i];
                        remain -= amount;
                    }
                    bends[ind < HeadLength * Bones.Length ? 0 : ind] = Quaternion.Lerp(Quaternion.identity, t.angle, remain / (Bones.Length * BendLength)) * bends[ind < HeadLength * Bones.Length ? 0 : ind];
                }
            }
            time = (time + Time.deltaTime * Mathf.PI * WiggleSpeed * (Movement ? Movement.SwimSpeed : 0.5f)) % (Mathf.PI * 2);
            for (int i = 0; i < Bones.Length; i++)
                Bones[i].localRotation = Quaternion.Euler(GetAngle(i, true), GetAngle(i), 0) * bends[i];
        }

        float GetAngle(int ind, bool cos = false)
        {
            if (!(cos ? WiggleY : WiggleX) || (float)ind / (Bones.Length - 1) <= HeadLength)
                return 0;
            Func<int, float> action;
            if (cos)
                action = x => Mathf.Cos(time - Mathf.PI * WiggleVolume * x / Bones.Length) * (x - HeadLength) / (Bones.Length - HeadLength) * WiggleAmplitude;
            else
                action = x => Mathf.Sin(time - Mathf.PI * WiggleVolume * x / Bones.Length) * (x - HeadLength) / (Bones.Length - HeadLength) * WiggleAmplitude;
            var a = action(ind);
            if ((ind - 1f) / (Bones.Length - 1) <= HeadLength)
                a -= action(ind - 1);
            return a;
        }
    }
}
