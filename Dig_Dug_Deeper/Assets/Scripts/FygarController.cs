using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FygarController : EnemyController
{
    [Header("Fire Settings")]
    [SerializeField] private GameObject firePrefab;
    [SerializeField] private float fireRange = 5f;
    [SerializeField] private float fireDuration = 0.5f;
    [SerializeField] private float fireCheckInterval = 2f;
    [SerializeField] private float fireCharging = 1f;

    private bool isPreparingToFire = false;
    private float fireTimer;
    private List<GameObject> activeFire = new List<GameObject>();

    // Animator parameter hash
    private static readonly int FireBreathTrigger = Animator.StringToHash("FireBreath");

    protected override void Update()
    {
        base.Update();

        if (currentState == EnemyState.Chasing && !isPreparingToFire)
        {
            fireTimer += Time.deltaTime;

            if (fireTimer >= fireCheckInterval)
            {
                fireTimer = 0f;

                if (CanFireAtPlayer(out Vector2 direction))
                {
                    isPreparingToFire = true; // Block further chasing
                    StartCoroutine(FireBreathRoutine(direction));
                }
            }
        }
        
        if (activeFire.Count > 0)
        {
            CheckPlayerInFire();
        }
    }

    private bool CanFireAtPlayer(out Vector2 direction)
    {
        direction = Vector2.zero;

        if (currentState == EnemyState.Ghost || currentState == EnemyState.Returning)
            return false;

        if (inflateStage > 0)
            return false;

        Vector2Int fygarPos = Vector2Int.RoundToInt(transform.position);
        Vector2Int playerPos = Vector2Int.RoundToInt(player.position);

        if (fygarPos.y != playerPos.y)
            return false;

        int dir = playerPos.x > fygarPos.x ? 1 : -1;
        direction = dir == 1 ? Vector2.right : Vector2.left;

        int distanceToPlayer = Mathf.Abs(playerPos.x - fygarPos.x);

        if (distanceToPlayer <= 1)
            return false;

        for (int i = 1; i <= fireRange; i++)
        {
            Vector2Int checkPos = fygarPos + new Vector2Int(dir * i, 0);

            if (!GridManager.Instance.IsTunnelAt(checkPos))
                return false;

            if (checkPos == playerPos)
                return true;
        }

        return false;
    }

    private IEnumerator FireBreathRoutine(Vector2 direction)
    {

        if (animator != null)
            animator.SetTrigger(FireBreathTrigger);

        currentState = EnemyState.FireBreath;
        isWanderingMoving = false;
        isPreparingToFire = true;

        rb.velocity = Vector2.zero;

        Vector2Int start = Vector2Int.RoundToInt(transform.position);
        yield return new WaitForSeconds(fireCharging);
        for (int i = 1; i <= fireRange; i++)
        {
            Vector2Int tile = start + Vector2Int.RoundToInt(direction) * i;
            if (!GridManager.Instance.IsTunnelAt(tile))
                break;

            Vector3 spawnPos = new Vector3(tile.x, tile.y, 0);
            GameObject fire = Instantiate(firePrefab, spawnPos, Quaternion.identity);

            // ▶ Flip the fire sprite based on spit direction
            SpriteRenderer fireSr = fire.GetComponent<SpriteRenderer>();
            if (fireSr != null)
            {
                // if direction.x is negative, flipX = true; else false
                fireSr.flipX = (direction.x < 0);
            }

            fire.AddComponent<FireTrigger>().Init(player);
            activeFire.Add(fire);
        }

        yield return new WaitForSeconds(fireDuration);

        foreach (GameObject f in activeFire)
            if (f != null) Destroy(f);
        activeFire.Clear();

        yield return new WaitForSeconds(0.5f);

        currentState = EnemyState.Chasing;
        isPreparingToFire = false;
    }

    private void CheckPlayerInFire()
    {
        Vector2 playerPos = player.position;

        foreach (GameObject fire in activeFire)
        {
            if (fire == null) continue;

            if (Vector2.Distance(fire.transform.position, playerPos) < 0.4f)
            {
                player.GetComponent<PlayerController>()?.CrushMe();
                break;
            }
        }
    }
}
