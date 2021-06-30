using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{

    public List<Gun> guns;

    public Material tracerMaterial;

    private Dictionary<GunType, Dictionary<string, object>> parameters = new Dictionary<GunType, Dictionary<string, object>>();

    public Gun EquippedGun { get; set; }
    private Quaternion _startRotation;
    private GunType? gunToEquip;

    public enum GunType
    {
        Rifle, Pistol
    }

    public void EquipGun(GunType? type)
    {
        if (EquippedGun != null)
        {
            if (type == EquippedGun.GetGunType()) return;
            EquippedGun.Unequip();
        }
        gunToEquip = type;
    }

    private void Start()
    {
        EquipGun(GunType.Rifle);
    }

    private bool cycleOnNextTimestep;

    private void FixedUpdate()
    {
        if ((EquippedGun == null && gunToEquip != null) || cycleOnNextTimestep)
        {
            cycleOnNextTimestep = false;
            if (EquippedGun != null)
            {
                var dict = new Dictionary<string, object>();
                foreach (var param in EquippedGun.animator.parameters)
                {
                    if (param.type == AnimatorControllerParameterType.Bool) dict[param.name] = EquippedGun.animator.GetBool(param.name);
                    if (param.type == AnimatorControllerParameterType.Float) dict[param.name] = EquippedGun.animator.GetFloat(param.name);
                    if (param.type == AnimatorControllerParameterType.Int) dict[param.name] = EquippedGun.animator.GetInteger(param.name);
                }
                parameters[EquippedGun.GetGunType()] = dict;
                Destroy(EquippedGun.gameObject);
            }
            foreach (var gun in guns)
            {
                if (gun.GetGunType() == gunToEquip)
                {
                    EquippedGun = Instantiate(gun, transform);
                    if (parameters.ContainsKey(EquippedGun.GetGunType()))
                    {
                        foreach (var param in parameters[EquippedGun.GetGunType()])
                        {
                            if (param.Value is bool) EquippedGun.animator.SetBool(param.Key, (bool)param.Value);
                            if (param.Value is float) EquippedGun.animator.SetFloat(param.Key, (float)param.Value);
                            if (param.Value is int) EquippedGun.animator.SetInteger(param.Key, (int)param.Value);
                        }
                    }
                    EquippedGun.animator.Update(0);
                    EquippedGun.WeaponManager = this;
                    _startRotation = EquippedGun.cameraBone.localRotation;
                    break;
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (EquippedGun != null)
        {
            var rot = EquippedGun.cameraBone.localRotation * Quaternion.Inverse(_startRotation);
            var euler = rot.eulerAngles;
            Game.Player.cameraParent.localRotation = Quaternion.Euler(euler.y, euler.z, euler.x);
        }
    }

    public abstract class Gun : MonoBehaviour
    {

        public WeaponManager WeaponManager { get; set; }

        public Camera viewModel;
        public Transform cameraBone;
        public GameObject rightHand;
        public GameObject leftHand;
        public Transform tracerStart;

        public Transform leftHandCenter;

        public Animator animator;

        public abstract GunType GetGunType();

        public Vector3 GetTracerStartWorldPosition()
        {
            var screen = viewModel.WorldToViewportPoint(tracerStart.position);
            screen.z = 1.2f;
            var world = Game.Player.camera.ViewportToWorldPoint(screen);
            return world;
        }

        public void Fire(QueryTriggerInteraction triggerInteraction)
        {
            var obj = new GameObject("Tracer");
            obj.AddComponent<TracerDecay>();
            var line = obj.AddComponent<LineRenderer>();
            var positions = new Vector3[2];
            positions[0] = GetTracerStartWorldPosition();
            positions[1] = Game.Player.camera.transform.position + (Game.Player.CrosshairDirection * 300);
            line.material = WeaponManager.tracerMaterial;
            line.endWidth = 0.1f;
            line.startWidth = 0.1f;
            line.SetPositions(positions);
            foreach (var hit in Physics.RaycastAll(Game.Player.camera.transform.position, Game.Player.CrosshairDirection, 300, 1, triggerInteraction))
            {
                if (!hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Kill Block"))
                {
                    var seeking = hit.collider.gameObject.GetComponent<SeekingDestructable>();
                    try
                    {
                        if (seeking == null) seeking = hit.collider.gameObject.transform.parent.gameObject.GetComponent<SeekingDestructable>();
                    }
                    catch (NullReferenceException)
                    {
                    }
                    if (seeking != null)
                    {
                        seeking.Hit(hit);
                    }
                }
            }
        }

        public void DoAbilityCatch()
        {
            animator.Play("AbilityCatch", 1, 0f);
        }

        public void LeftWallStart()
        {
            if (animator.GetCurrentAnimatorStateInfo(1).normalizedTime > 1)
                animator.Play("LeftWallTouch", 1, 0f);
        }

        public void RightWallStart()
        {
            if (animator.GetCurrentAnimatorStateInfo(1).normalizedTime > 1)
                animator.Play("RightWallTouch", 1, 0f);
        }

        public bool boomerangVisible = true;
        public bool boomerangAvailable = true;

        public void BoomerangCatch(int direction)
        {
            if (direction == 0)
            {
                animator.Play("BoomerangCatchRight", 1, 0f);
            }
            else if (direction == 1)
            {
                animator.Play("BoomerangCatchLeft", 1, 0f);
            }
            else
            {
                animator.Play("BoomerangCatchForward", 1, 0f);
            }
            boomerangVisible = true;
            boomerangAvailable = true;
        }

        public void WallStop()
        {
            if (animator.GetCurrentAnimatorStateInfo(1).IsName("RightWallTouch"))
            {
                animator.Play("RightWallTouchReverse", 1, 0f);
            }
            if (animator.GetCurrentAnimatorStateInfo(1).IsName("LeftWallTouch"))
            {
                animator.Play("LeftWallTouchReverse", 1, 0f);
            }
        }

        public void Unequip()
        {
            animator.SetBool("Unequip", true);
        }

        public void UnequipFinished()
        {
            WeaponManager.cycleOnNextTimestep = true;
            animator.SetBool("Unequip", false);
        }

    }

}
