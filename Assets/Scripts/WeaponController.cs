using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public Transform cam;
    public Combatant owner;
    public FpsPlayer player;
    public Transform muzzle;
    public Light flash;
    public Transform model;
    public float damage = 22f;
    public float fireDelay = 0.11f;
    public float reloadTime = 1.3f;
    public float range = 150f;
    public float spread = 0.012f;
    public int magSize = 30;

    public int Ammo { get; private set; }
    public bool Reloading { get; private set; }

    float nextShot, reloadEnd, flashOff;
    Vector3 modelRest;

    void Start()
    {
        Ammo = magSize;
        if (model != null) modelRest = model.localPosition;
        if (flash != null) flash.enabled = false;
    }

    public void AddMag(int n) { magSize += n; Ammo += n; }

    public void Reload()
    {
        if (Reloading || Ammo >= magSize) return;
        Reloading = true;
        reloadEnd = Time.time + reloadTime;
    }

    void Update()
    {
        if (flash != null && flash.enabled && Time.time > flashOff) flash.enabled = false;
        if (model != null)
        {
            Vector3 rest = Reloading ? modelRest + new Vector3(0f, -0.25f, 0f) : modelRest;
            model.localPosition = Vector3.Lerp(model.localPosition, rest, Time.deltaTime * 14f);
        }
        if (owner == null || !owner.Alive) return;
        if (MatchManager.I != null && !MatchManager.I.Playing) return;

        if (Reloading && Time.time >= reloadEnd) { Reloading = false; Ammo = magSize; }
        bool want = player != null && player.FireHeld;
        if (want && !Reloading && Time.time >= nextShot)
        {
            if (Ammo <= 0) Reload(); else Fire();
        }
    }

    void Fire()
    {
        nextShot = Time.time + fireDelay;
        Ammo--;
        Vector3 dir = cam.forward + cam.right * Random.Range(-spread, spread) + cam.up * Random.Range(-spread, spread);
        dir.Normalize();
        Vector3 end = cam.position + dir * range;
        RaycastHit h;
        if (Physics.Raycast(cam.position, dir, out h, range, ~0, QueryTriggerInteraction.Ignore))
        {
            end = h.point;
            Combatant c = h.collider.GetComponentInParent<Combatant>();
            if (c != null && c.team != owner.team && c.Alive) c.TakeDamage(damage, owner);
        }
        Vector3 from = muzzle != null ? muzzle.position : cam.position;
        Tracer.Draw(from, end, new Color(0.4f, 0.95f, 1f));
        if (model != null) model.localPosition += new Vector3(0f, 0.01f, -0.07f);
        if (flash != null) { flash.enabled = true; flashOff = Time.time + 0.04f; }
        Sfx.Shot(cam.position, 0.45f);
        if (Ammo <= 0) Reload();
    }
}
