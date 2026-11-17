using UnityEngine;

public class AutoProjectile : MonoBehaviour
{
    public float lifeTime = 3f;

    private Vector3 _direction;
    private float _speed;
    private float _damage;
    private float _knockback;
    private bool _isCrit;
    private LayerMask _targetLayer;
    private float _timer;

    public void Initialize(Vector3 direction, float speed, float damage, float knockback, bool isCrit, LayerMask targetLayer)
    {
        _direction = direction.normalized;
        _speed = speed;
        _damage = damage;
        _knockback = knockback;
        _isCrit = isCrit;
        _targetLayer = targetLayer;
        _timer = lifeTime;
    }

    private void Update()
    {
        float moveStep = _speed * Time.deltaTime;

        // Raycast corto para evitar que proyectiles rápidos atraviesen enemigos entre frames
        if (Physics.Raycast(transform.position, _direction, out RaycastHit hit, moveStep + 0.2f, _targetLayer, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.TryGetComponent(out InDamage damageable))
            {
                damageable.TakeDamage(_damage, hit.point, _direction * _knockback, _isCrit);
            }
            Destroy(gameObject);
            return;
        }

        transform.position += _direction * moveStep;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
