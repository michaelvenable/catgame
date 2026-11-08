using System;
using UnityEngine;

namespace CatGame
{
    // Drives the cat's skeleton from code: a speed-dependent gait (walk -> trot -> gallop) with planted feet,
    // a steady head, an expressive tail, and air poses for jumps. Every LateUpdate starts from the rest pose,
    // so nothing accumulates between frames.
    [RequireComponent(typeof(CatController))]
    public class CatProceduralMotion : MonoBehaviour
    {
        [Serializable]
        public class Leg
        {
            public Transform upper;
            public Transform lower;
            [Tooltip("Wrist (front) or hock (hind): the joint the two-bone IK places.")]
            public Transform ankle;
            [Tooltip("Paw bone; its origin sits on the ground.")]
            public Transform paw;
        }

        // Leg order: front left, front right, hind left, hind right. Right legs mirror their left partner's rest data.
        const int FrontLeft = 0, HindLeft = 2;
        static readonly float[] WalkOffsets = { 0.25f, 0.75f, 0f, 0.5f };   // lateral sequence: HL, FL, HR, FR
        static readonly float[] TrotOffsets = { 0.5f, 0f, 0f, 0.5f };       // diagonal pairs
        static readonly float[] GallopOffsets = { 0.5f, 0.6f, 0f, 0.1f };   // hind pair, then front pair

        [Header("Skeleton")]
        [SerializeField] Transform bodyRoot;
        [SerializeField] Transform neck;
        [SerializeField] Leg[] legs = new Leg[4];
        [Tooltip("Tail bones from base to tip.")]
        [SerializeField] Transform[] tail;

        [Header("Gait")]
        [SerializeField] float trotSpeed = 2.4f;
        [SerializeField] float gallopSpeed = 4.8f;
        [Tooltip("Distance the body travels per full gait cycle, at standstill and at top speed. Long strides " +
                 "at walking pace outreach the legs, so stride grows with speed^strideCurve.")]
        [SerializeField] Vector2 strideLength = new(0.4f, 1.6f);
        [SerializeField] float strideCurve = 1.5f;
        [SerializeField] float topSpeed = 7f;
        [SerializeField] float stepHeight = 0.07f;
        [Tooltip("Steps taken to re-centre the feet after stopping or turning on the spot.")]
        [SerializeField] float settleStepRate = 1.6f;
        [SerializeField] float settleDistance = 0.05f;
        [SerializeField] LayerMask groundLayers = ~0;

        [Header("Body")]
        [Tooltip("Cats walk slightly crouched; also keeps the legs bent so the IK has room.")]
        [SerializeField] float crouch = 0.06f;
        [SerializeField] float breatheAmount = 0.004f;
        [SerializeField] float breatheFrequency = 0.6f;
        [SerializeField] float landSquash = 0.08f;
        [SerializeField] float landRecovery = 0.22f;

        [Header("Tail")]
        [SerializeField] float tailSwayDegrees = 8f;
        [SerializeField] float tailSwayFrequency = 0.3f;
        [Tooltip("How far the tail lowers toward horizontal at a gallop, spread along the chain.")]
        [SerializeField] float tailGallopDrop = 35f;
        [SerializeField] float tailTurnLag = 0.12f;

        CatController controller;
        Collider[] selfColliders;
        readonly RaycastHit[] hitBuffer = new RaycastHit[8];

        Transform[] posedBones;
        Vector3[] restLocalPositions;
        Quaternion[] restLocalRotations;

        // Rest data, in this transform's space.
        Vector3[] homeFeet;
        Vector3[] distal;          // ankle -> paw
        Vector3[] bendHint;        // which way the knee/elbow points
        Quaternion[] pawStand;     // paw rotation when planted
        Vector3 bodyPivot;

        // Gait state.
        float phase;
        readonly float[] offsets = new float[4];
        float gallopWeight, trotWeight;
        readonly bool[] swinging = new bool[4];
        readonly Vector3[] plantedAt = new Vector3[4];
        readonly Vector3[] swingFrom = new Vector3[4];
        readonly float[] swingStart = new float[4];      // swing progress (0..1) when this step began
        readonly Vector3[] landFrom = new Vector3[4];
        float landBlend = 1f;
        bool moving;
        readonly Vector3[] feet = new Vector3[4];
        // Air poses are held in this transform's space so they don't trail behind a fast-moving body.
        readonly Vector3[] airFeet = new Vector3[4];

        // How the body is rotated relative to its rest pose this frame (pitch and roll from PoseBody).
        Quaternion bodyTilt = Quaternion.identity;
        float bodyPitch, bodyRoll;

        bool airborne;
        float airTime;
        float squash;
        float lastYaw;
        float turnRate;
        float tailLag;

        void Awake()
        {
            controller = GetComponent<CatController>();
            selfColliders = GetComponentsInChildren<Collider>();
            CaptureRest();
            Array.Copy(WalkOffsets, offsets, 4);
            for (int i = 0; i < 4; i++)
            {
                plantedAt[i] = GroundPoint(transform.TransformPoint(homeFeet[i]));
                feet[i] = plantedAt[i];
            }
            lastYaw = transform.eulerAngles.y;
        }

        void CaptureRest()
        {
            var bones = new System.Collections.Generic.List<Transform> { bodyRoot, neck };
            foreach (Leg leg in legs)
                bones.AddRange(new[] { leg.upper, leg.lower, leg.ankle, leg.paw });
            bones.AddRange(tail);
            posedBones = bones.ToArray();
            restLocalPositions = Array.ConvertAll(posedBones, b => b.localPosition);
            restLocalRotations = Array.ConvertAll(posedBones, b => b.localRotation);

            homeFeet = new Vector3[4];
            distal = new Vector3[4];
            bendHint = new Vector3[4];
            pawStand = new Quaternion[4];
            foreach (int left in new[] { FrontLeft, HindLeft })
            {
                Leg leg = legs[left];
                Vector3 upper = Local(leg.upper.position), lower = Local(leg.lower.position);
                Vector3 ankle = Local(leg.ankle.position), paw = Local(leg.paw.position);

                homeFeet[left] = new Vector3(paw.x, 0f, paw.z);
                distal[left] = paw - ankle;
                Vector3 reach = (ankle - upper).normalized;
                bendHint[left] = Vector3.ProjectOnPlane(lower - upper, reach).normalized;
                pawStand[left] = Quaternion.Inverse(transform.rotation) * leg.paw.rotation;

                int right = left + 1;
                homeFeet[right] = Mirror(homeFeet[left]);
                distal[right] = Mirror(distal[left]);
                bendHint[right] = Mirror(bendHint[left]);
                // The right paw's rest pose is mid-step; rotate its bone axis onto the mirrored left paw's axis.
                Quaternion rightRest = Quaternion.Inverse(transform.rotation) * legs[right].paw.rotation;
                Vector3 leftAxis = pawStand[left] * Vector3.up;
                pawStand[right] = Quaternion.FromToRotation(rightRest * Vector3.up, Mirror(leftAxis)) * rightRest;
            }

            bodyPivot = (Local(legs[FrontLeft].upper.position) + Local(legs[HindLeft].upper.position)) * 0.5f;
            bodyPivot.x = 0f;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            for (int i = 0; i < posedBones.Length; i++)
                posedBones[i].SetLocalPositionAndRotation(restLocalPositions[i], restLocalRotations[i]);

            Vector3 velocity = controller.Velocity;
            Vector3 planar = Vector3.ProjectOnPlane(velocity, Vector3.up);
            float speed = planar.magnitude;
            moving = speed > 0.05f;

            float yaw = transform.eulerAngles.y;
            turnRate = Mathf.Lerp(turnRate, Mathf.DeltaAngle(lastYaw, yaw) / dt, 1f - Mathf.Exp(-10f * dt));
            lastYaw = yaw;

            UpdateAirState(dt, velocity.y);
            UpdateGaitBlend(speed, dt);

            float cycle = Mathf.Lerp(strideLength.x, strideLength.y, Mathf.Pow(Mathf.Clamp01(speed / topSpeed), strideCurve));
            float duty = Mathf.Lerp(Mathf.Lerp(0.55f, 0.5f, trotWeight), 0.32f, gallopWeight);
            float frequency = speed / cycle;
            if (!airborne && frequency < settleStepRate && NeedsSettling())
                frequency = settleStepRate;
            phase = Mathf.Repeat(phase + frequency * dt, 1f);

            PoseBody(speed, velocity.y);
            PoseHead();
            for (int i = 0; i < 4; i++)
            {
                if (airborne)
                    feet[i] = AirFoot(i, velocity.y, dt);
                else
                    feet[i] = GroundFoot(i, planar, duty, frequency);
                SolveLeg(i);
            }
            PoseTail(speed, dt);
        }

        void UpdateAirState(float dt, float verticalSpeed)
        {
            bool groundedNow = controller.IsGrounded;
            if (!groundedNow)
                airTime += dt;
            else
                airTime = 0f;

            // Short ungrounded blips (steps, ramp seams) are not jumps.
            bool nowAirborne = !groundedNow && (airTime > 0.08f || verticalSpeed > 0.5f);
            if (!airborne && nowAirborne)
            {
                for (int i = 0; i < 4; i++)
                    airFeet[i] = Local(feet[i]);
            }
            else if (airborne && !nowAirborne)
            {
                squash = 1f;
                landBlend = 0f;
                for (int i = 0; i < 4; i++)
                {
                    swinging[i] = false;
                    landFrom[i] = feet[i];
                    plantedAt[i] = GroundPoint(feet[i]);
                }
            }
            airborne = nowAirborne;
            squash = Mathf.MoveTowards(squash, 0f, dt / landRecovery);
            landBlend = Mathf.MoveTowards(landBlend, 1f, dt / 0.08f);
        }

        void UpdateGaitBlend(float speed, float dt)
        {
            float k = 1f - Mathf.Exp(-6f * dt);
            trotWeight = Mathf.Lerp(trotWeight, speed >= trotSpeed ? 1f : 0f, k);
            gallopWeight = Mathf.Lerp(gallopWeight, speed >= gallopSpeed ? 1f : 0f, k);
            for (int i = 0; i < 4; i++)
            {
                float target = Mathf.Lerp(Mathf.Lerp(WalkOffsets[i], TrotOffsets[i], trotWeight), GallopOffsets[i], gallopWeight);
                offsets[i] = Mathf.Repeat(offsets[i] + Mathf.DeltaAngle(offsets[i] * 360f, target * 360f) / 360f * k, 1f);
            }
        }

        bool NeedsSettling()
        {
            for (int i = 0; i < 4; i++)
            {
                if (swinging[i] || !IsHome(i))
                    return true;
            }
            return false;
        }

        bool IsHome(int i)
        {
            Vector3 home = transform.TransformPoint(homeFeet[i]);
            return Vector3.ProjectOnPlane(plantedAt[i] - home, Vector3.up).sqrMagnitude <= settleDistance * settleDistance;
        }

        void PoseBody(float speed, float verticalSpeed)
        {
            const float TwoPi = Mathf.PI * 2f;
            float moving = Mathf.Clamp01(speed / 0.5f);
            float t = Time.time;

            // Walk/trot: a small bob per footfall. Gallop: one big rock per stride.
            float bobAmp = Mathf.Lerp(Mathf.Lerp(0.008f, 0.015f, trotWeight), 0.03f, gallopWeight) * moving;
            float bobFreq = Mathf.Lerp(2f, 1f, gallopWeight);
            float height = -crouch * moving
                - Mathf.Abs(Mathf.Sin(phase * TwoPi * bobFreq * 0.5f)) * bobAmp
                + Mathf.Sin(t * breatheFrequency * TwoPi) * breatheAmount
                - squash * squash * landSquash;

            float pitch = Mathf.Sin(phase * TwoPi) * 6f * gallopWeight * moving;
            float roll = Mathf.Sin(phase * TwoPi) * 2f * (1f - gallopWeight) * moving;
            if (airborne)
                pitch = -Mathf.Clamp(verticalSpeed * 2.5f, -18f, 18f);

            Vector3 pivot = transform.TransformPoint(bodyPivot);
            bodyRoot.position += transform.up * height;
            bodyRoot.RotateAround(pivot, transform.right, pitch);
            bodyRoot.RotateAround(pivot, transform.forward, roll);
            bodyPitch = pitch;
            bodyRoll = roll;
            bodyTilt = bodyRoot.rotation * Quaternion.Inverse(bodyRoot.parent.rotation * restLocalRotations[0]);
        }

        void PoseHead()
        {
            // Keep the head level while the body rocks, and lead into turns.
            float lead = Mathf.Clamp(turnRate * 0.06f, -20f, 20f);
            neck.rotation = Quaternion.AngleAxis(lead, transform.up)
                * Quaternion.AngleAxis(-bodyRoll, transform.forward)
                * Quaternion.AngleAxis(-bodyPitch * 0.7f, transform.right)
                * neck.rotation;
        }

        Vector3 GroundFoot(int i, Vector3 planarVelocity, float duty, float frequency)
        {
            float legPhase = Mathf.Repeat(phase + offsets[i], 1f);
            bool inSwing = legPhase >= duty && frequency > 0f;
            Vector3 home = transform.TransformPoint(homeFeet[i]);

            float s = Mathf.InverseLerp(duty, 1f, legPhase);
            // Start a step only early in the leg's swing window (a late start would snap the paw), and while
            // settling, only for paws that are still away from home; otherwise the cat marches in place.
            if (inSwing && !swinging[i] && s < 0.5f && (moving || !IsHome(i)))
            {
                swinging[i] = true;
                swingFrom[i] = plantedAt[i];
                swingStart[i] = s;
            }

            if (!swinging[i])
                return Landing(i, plantedAt[i]);

            // Land ahead of the hip so the stance phase carries the foot back past it.
            float cycleTime = 1f / Mathf.Max(frequency, 0.01f);
            // Once the phase has wrapped the swing is over: no swing time remains.
            float remaining = inSwing ? (1f - legPhase) * cycleTime : 0f;
            Vector3 target = GroundPoint(home + planarVelocity * (remaining + duty * cycleTime * 0.5f));

            if (!inSwing)
            {
                swinging[i] = false;
                plantedAt[i] = target;
                return target;
            }

            float progress = Mathf.InverseLerp(swingStart[i], 1f, s);
            float eased = progress * progress * (3f - 2f * progress);
            Vector3 foot = Vector3.Lerp(swingFrom[i], target, eased);
            return Landing(i, foot + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * stepHeight));
        }

        // Eases the paws from their air pose onto the ground over the first frames after touchdown.
        Vector3 Landing(int i, Vector3 foot) => Vector3.Lerp(landFrom[i], foot, landBlend);

        Vector3 AirFoot(int i, float verticalSpeed, float dt)
        {
            // Rising: front paws reach up and forward, hind legs trail from the push-off.
            // Falling: front paws reach down for the landing, hind legs tuck under.
            bool front = i < 2;
            float falling = Mathf.InverseLerp(2f, -3f, verticalSpeed);
            Vector3 rise = front ? new Vector3(0f, 0.16f, 0.1f) : new Vector3(0f, 0.1f, -0.16f);
            Vector3 fall = front ? new Vector3(0f, 0.02f, 0.14f) : new Vector3(0f, 0.12f, 0.06f);
            Vector3 target = homeFeet[i] + Vector3.Lerp(rise, fall, falling);
            airFeet[i] = Vector3.Lerp(airFeet[i], target, 1f - Mathf.Exp(-18f * dt));
            return transform.TransformPoint(airFeet[i]);
        }

        void SolveLeg(int i)
        {
            Leg leg = legs[i];
            // On the ground, distal segments and paws stay upright; in the air they follow the tilted body.
            Quaternion frame = airborne ? bodyTilt * transform.rotation : transform.rotation;
            Vector3 footTarget = feet[i];
            Vector3 ankleTarget = footTarget - frame * distal[i];

            Vector3 a = leg.upper.position, b = leg.lower.position, c = leg.ankle.position;
            float upperLength = Vector3.Distance(a, b), lowerLength = Vector3.Distance(b, c);
            Vector3 toTarget = ankleTarget - a;
            float distance = Mathf.Clamp(toTarget.magnitude, 0.001f, (upperLength + lowerLength) * 0.999f);
            Vector3 direction = toTarget.normalized;
            Vector3 bend = Vector3.ProjectOnPlane(bodyTilt * transform.rotation * bendHint[i], direction).normalized;

            float cosUpper = Mathf.Clamp((upperLength * upperLength + distance * distance - lowerLength * lowerLength) / (2f * upperLength * distance), -1f, 1f);
            float sinUpper = Mathf.Sqrt(1f - cosUpper * cosUpper);
            Vector3 knee = a + (direction * cosUpper + bend * sinUpper) * upperLength;
            leg.upper.rotation = Quaternion.FromToRotation(b - a, knee - a) * leg.upper.rotation;

            Vector3 reached = a + direction * distance;
            leg.lower.rotation = Quaternion.FromToRotation(leg.ankle.position - leg.lower.position, reached - leg.lower.position) * leg.lower.rotation;
            leg.ankle.rotation = Quaternion.FromToRotation(leg.paw.position - leg.ankle.position, footTarget - leg.ankle.position) * leg.ankle.rotation;

            // Planted paws lie flat; swinging paws curl back.
            float curl = swinging[i] || airborne ? 25f : 0f;
            leg.paw.rotation = Quaternion.AngleAxis(curl, transform.right) * frame * pawStand[i];
        }

        void PoseTail(float speed, float dt)
        {
            const float TwoPi = Mathf.PI * 2f;
            tailLag = Mathf.Lerp(tailLag, Mathf.Clamp(-turnRate * tailTurnLag, -30f, 30f), 1f - Mathf.Exp(-5f * dt));
            // Positive drop lowers the tail toward horizontal; in the air it lifts for balance.
            float drop = tailGallopDrop * gallopWeight + (airborne ? -15f : 0f);
            float t = Time.time;
            for (int i = 0; i < tail.Length; i++)
            {
                float along = (float)i / Mathf.Max(1, tail.Length - 1);
                // The sway travels down the tail, so the tip trails the base.
                float sway = Mathf.Sin((t * tailSwayFrequency - along * 0.35f) * TwoPi) * tailSwayDegrees / tail.Length * 2f;
                float yaw = sway + tailLag / tail.Length;
                // Rotating about +right swings "up" toward "forward", so lowering a back-pointing tail is negative.
                tail[i].rotation = Quaternion.AngleAxis(yaw, transform.up)
                    * Quaternion.AngleAxis(-drop / tail.Length, transform.right)
                    * tail[i].rotation;
            }
        }

        Vector3 GroundPoint(Vector3 point)
        {
            // Start just above step height, so a foot beside a crate doesn't plant on top of it.
            Vector3 origin = new(point.x, transform.position.y + 0.35f, point.z);
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, hitBuffer, 1.2f, groundLayers, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            Vector3 result = new(point.x, transform.position.y, point.z);
            for (int i = 0; i < count; i++)
            {
                if (Array.IndexOf(selfColliders, hitBuffer[i].collider) >= 0 || hitBuffer[i].distance >= best)
                    continue;
                best = hitBuffer[i].distance;
                result = hitBuffer[i].point;
            }
            return result;
        }

        Vector3 Local(Vector3 world) => transform.InverseTransformPoint(world);
        static Vector3 Mirror(Vector3 v) => new(-v.x, v.y, v.z);
    }
}
