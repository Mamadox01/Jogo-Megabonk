using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class EnemyController : MonoBehaviour, InDamage
{
    [Header("Configuración")]
    public EnemyData data;
    public Renderer meshRenderer;

    private Rigidbody _rb;
    private Transform _target;
    private IObjectPool<EnemyController> _pool;

    private float _currentHp;
    private Vector3 _knockbackVelocity;
    private bool _isDead;

    private MaterialPropertyBlock _propBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private Color _originalColor = Color.white;
    private Coroutine _flashRoutine;

    // Evento para conectar luego el drop de gemas de XP o partículas
    public static event Action<Vector3, int> OnEnemyKilled;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<Renderer>();

        _propBlock = new MaterialPropertyBlock();
        if (meshRenderer != null && meshRenderer.sharedMaterial != null && meshRenderer.sharedMaterial.HasProperty(BaseColorId))
        {
            _originalColor = meshRenderer.sharedMaterial.GetColor(BaseColorId);
        }
    }

    private void Start()
    {
        // Fallback por si colocas un enemigo manualmente en la escena sin usar el Spawner
        if (_target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _target = playerObj.transform;
        }

        if (_currentHp <= 0f && data != null)
        {
            _currentHp = data.maxHp;
        }
    }

    public void Initialize(Transform target, IObjectPool<EnemyController> pool, float hpMultiplier = 1f)
    {
        _target = target;
        _pool = pool;
        _isDead = false;
        _knockbackVelocity = Vector3.zero;
        _currentHp = (data != null ? data.maxHp : 40f) * hpMultiplier;
        SetFlashColor(_originalColor);
    }

    private void FixedUpdate()
    {
        if (_isDead || _target == null) return;

        float speed = data != null ? data.moveSpeed : 4.5f;

        Vector3 toTarget = _target.position - transform.position;
        toTarget.y = 0f;

        Vector3 moveDir = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : Vector3.zero;

        if (moveDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRot, 10f * Time.fixedDeltaTime));
        }

        // Mantiene la velocidad vertical (gravedad) para subir y bajar rampas suavemente
        Vector3 finalHorizontal = (moveDir * speed) + _knockbackVelocity;
        _rb.linearVelocity = new Vector3(finalHorizontal.x, _rb.linearVelocity.y, finalHorizontal.z);

        _knockbackVelocity = Vector3.Lerp(_knockbackVelocity, Vector3.zero, 10f * Time.fixedDeltaTime);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (_isDead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.gameObject.TryGetComponent(out PlayerHealth playerHealth))
            {
                float dmg = data != null ? data.contactDamage : 10f;
                Vector3 hitPoint = collision.GetContact(0).point;
                playerHealth.TakeDamage(dmg, hitPoint, Vector3.zero, false);
            }
        }
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 knockbackForce, bool isCrit)
    {
        if (_isDead) return;

        _currentHp -= amount;

        float resistance = data != null ? data.knockbackResistance : 1f;
        if (resistance > 0f)
        {
            _knockbackVelocity += knockbackForce / resistance;
            _knockbackVelocity.y = 0f;
        }

        TriggerHitFlash(isCrit ? Color.yellow : Color.white);

        if (_currentHp <= 0f)
        {
            Die();
        }
    }

    private void TriggerHitFlash(Color flashColor)
    {
        if (meshRenderer == null) return;
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashCoroutine(flashColor));
    }

    private IEnumerator FlashCoroutine(Color flashColor)
    {
        SetFlashColor(flashColor);
        yield return new WaitForSeconds(0.08f);
        SetFlashColor(_originalColor);
    }

    private void SetFlashColor(Color color)
    {
        if (meshRenderer == null) return;
        meshRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(BaseColorId, color);
        meshRenderer.SetPropertyBlock(_propBlock);
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        int xp = data != null ? data.xpDropAmount : 10;
        OnEnemyKilled?.Invoke(transform.position, xp);

        if (_pool != null)
        {
            _pool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
