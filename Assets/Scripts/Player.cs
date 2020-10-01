using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    /* Ways that a player can move */
    enum Direction {
        Left,
        Right,
        Down,
        Up
    }

    /* Provided objects */
    public GameObject tailSample;
    public GameObject foodSample;
    public Text gameOverText;
    
    /* Zoom the camera by this amount */
    public float screenZoom = 1f;

    /* Interval in seconds to move one full unit */
    public float movementInterval = 0.1f;

    /* Number of tail pieces to start with */
    public int startingTailLength = 3;

    /* Current direction the head is facing */
    private Direction direction;

    /* The direction to apply when the movement loop happens */
    private Direction pendingDirection = Direction.Up;
    
    /* Touchscreen movement information */
    private Vector2 touchStart;
    private Vector2 touchEnd;

    /* How far a drag length must be to consider a swipe */
    private float swipeThreshold = 100f;

    /* Last position that the head was in */
    private Vector3 lastHeadPosition;

    /* All tail pieces */
    private List<GameObject> tails = new List<GameObject>();

    void Start() {
        /* Handle screen zoom preferences */
        Camera.main.orthographicSize /= screenZoom;

        /* Move the player once every second */
        Invoke("MovementLoop", movementInterval);

        /* Start us off with a tail */
        for (int i = 0; i < startingTailLength; i++)
            AddTailPiece();
        
        /* Spawn a food */
        AddFood();
    }

    private void AddTailPiece() {
        Vector3 spawnLocation = Vector3.zero;

        /* By default, spawn just out of sight */
        spawnLocation.y = Camera.main.orthographicSize + 1;

        /* Prefer to spawn on top of newest piece */
        if (tails.Count > 0)
            spawnLocation = tails[tails.Count - 1].transform.position;

        /* Spawn */
        GameObject tail = Instantiate(tailSample, spawnLocation, Quaternion.identity);
        tails.Add(tail);
    }

    private void AddFood() {
        /* Get bounds of the screen */
        Camera camera = Camera.main;
        int halfHeight = Mathf.FloorToInt(camera.orthographicSize) - 1;
        int halfWidth = Mathf.FloorToInt(camera.aspect * halfHeight) - 1;

        /* Loop until we find an available position */
        int randX, randY;
        Vector3 foodPosition;
        while (true) {
            randX = Mathf.RoundToInt(Random.Range(-halfWidth, halfWidth));
            randY = Mathf.RoundToInt(Random.Range(-halfHeight, halfHeight));
            foodPosition = new Vector3(randX, randY);

            /* Bail if intersecting the head */
            if (transform.position == foodPosition)
                continue;
            
            /* Bail if intersecting the tail */
            foreach (GameObject tail in tails)
                if (tail.transform.position == foodPosition)
                    continue;
            
            /* All clear, back out and spawn */
            break;
        }

        /* Spawn the food */
        Instantiate(foodSample, foodPosition, Quaternion.identity);
    }

    private void MovementLoop() {
        /* Update our current direction to the user requested direction */
        direction = pendingDirection;

        /* Save the location of the head before we move */
        lastHeadPosition = transform.position;
        
        /* Calculate new position */
        Vector3 newPosition = transform.position;
        switch (direction) {
            case Direction.Left:
                newPosition.x--;
                break;
            case Direction.Right:
                newPosition.x++;
                break;
            case Direction.Down:
                newPosition.y--;
                break;
            case Direction.Up:
                newPosition.y++;
                break;
        }

        /* Move the head */
        transform.position = newPosition;

        /* Update the tail pieces */
        MigrateTail();

        /* Restart the drag so that the user does not need to lift their finger */
        touchStart = touchEnd;

        /* Make sure we schedule the next loop fresh */
        CancelInvoke("MovementLoop");
        Invoke("MovementLoop", movementInterval);
    }

    private void UpdateDirection() {
        /* Horizontal keyboard input */
        if (Input.GetKeyDown(KeyCode.A))
            pendingDirection = Direction.Left;
        else if (Input.GetKeyDown(KeyCode.D))
            pendingDirection = Direction.Right;

        /* Vertical keyboard input */
        if (Input.GetKeyDown(KeyCode.S))
            pendingDirection = Direction.Down;
        else if (Input.GetKeyDown(KeyCode.W))
            pendingDirection = Direction.Up;
        
        /* Horizontal touch input */
        if (Mathf.Abs(touchEnd.x - touchStart.x) > swipeThreshold) {
            if (touchEnd.x - touchStart.x < 0)
                pendingDirection = Direction.Left;
            if (touchEnd.x - touchStart.x > 0)
                pendingDirection = Direction.Right;
        }
        
        /* Vertical touch input */
        if (Mathf.Abs(touchEnd.y - touchStart.y) > swipeThreshold) {
            if (touchEnd.y - touchStart.y < 0)
                pendingDirection = Direction.Down;
            if (touchEnd.y - touchStart.y > 0)
                pendingDirection = Direction.Up;
        }

        /* Detect contradictions in movement and fallback */
        if ((direction == Direction.Left && pendingDirection == Direction.Right) ||
            (direction == Direction.Right && pendingDirection == Direction.Left) ||
            (direction == Direction.Down && pendingDirection == Direction.Up) ||
            (direction == Direction.Up && pendingDirection == Direction.Down))
            pendingDirection = direction;
    }

    private void MigrateTail() {
        /* Make sure we even have a tail */
        if (tails.Count == 0)
            return;

        /* Move the tail pieces from newest to oldest */
        for (int i = tails.Count - 1; i > 0; i--)
            tails[i].transform.position = tails[i - 1].transform.position;

        /* Move the first tail piece to the last head position */
        tails[0].transform.position = lastHeadPosition;
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        /* If a head and tail collide, it's game over */
        if (tag == "Head" && other.gameObject.tag == "Tail" &&
            transform.position == other.gameObject.transform.position)
            GameOver();

        /* If a head and food collide, add a tail piece and spawn a new food */
        if (tag == "Head" && other.gameObject.tag == "Food" &&
            transform.position == other.gameObject.transform.position) {
            /* Destroy the old food */
            Destroy(other.gameObject);
            AddTailPiece();
            AddFood();
        }
    }

    private void Restart() {
        /* Reload the scene to start over */
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GameOver() {
        /* Show Game Over text */
        gameOverText.gameObject.SetActive(true);

        /* Cancel movement loop so the head does not continue to move */
        CancelInvoke("MovementLoop");
        
        /* Wait a few seconds and restart the game */
        Invoke("Restart", 3f);
    }

    private void Update() {
        /* Keep track of the user's touch input */
        Touch[] touches = Input.touches;
        if (touches.Length >= 1) {
            Touch touch = touches[0];
            switch (touch.phase) {
                case TouchPhase.Began:
                    touchStart = touch.position;
                    touchEnd = touch.position;
                    break;
                case TouchPhase.Moved:
                    touchEnd = touch.position;
                    break;
                case TouchPhase.Ended:
                    touchEnd = touch.position;
                    break;
            }
        }

        /* Detect and update the direction we should go next */
        UpdateDirection();

        Camera camera = Camera.main;
        int halfHeight = Mathf.FloorToInt(camera.orthographicSize);
        int halfWidth = Mathf.FloorToInt(camera.aspect * halfHeight);

        /* Make sure we are not out of bounds, otherwise it's game over */
        if (Mathf.Abs(transform.position.x) > halfWidth ||
            Mathf.Abs(transform.position.y) > halfHeight)
            GameOver();
    }
}
