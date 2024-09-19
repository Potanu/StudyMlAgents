using TMPro;
using UnityEngine;

public class StudySceneManager : MonoBehaviour
{
    public enum State
    {
        ROTATE,
        MOVE
    }

    [SerializeField]
    private Cube cube = null;                   // 操作する立方体
    [SerializeField]
    private Transform areaTransform = null;     // 障害物
	[SerializeField]
	private float rotateSpeed = 120.0f;          // 操作キューブの回転速度（/s）
    [SerializeField]
    private float moveSpeed = 1.0f;             // 操作キューブの移動時間（s)
    [SerializeField]
    private TextMeshProUGUI timerText = null;

	public State state { get; private set; } = State.ROTATE;

    private float rotateCounter = 0.0f;
    private float moveCounter = 0.0f;

    private readonly float ROTATE_TIME_LIMIT = 10.0f;   // 回転操作時間

	private void Start()
    {
        SetAreaRotate();
		cube.SetRotateSpeed(rotateSpeed);

        if (timerText != null)
		{
            timerText.text = ROTATE_TIME_LIMIT.ToString("F2");
		}
	}

	private void FixedUpdate()
	{
        float deltaTime = Time.fixedDeltaTime;

		switch (state)
        {
            case State.ROTATE:
                if (rotateCounter >= ROTATE_TIME_LIMIT)
                {
                    cube.SetReward(-1.0f);
                    GoAction();
                    break;
                }

				if (!cube.IsHitRay())
				{
					float reward = ROTATE_TIME_LIMIT - rotateCounter;
					cube.SetReward(reward);

					GoAction();
					break;
				}

				cube.RequestDecision();
                rotateCounter += deltaTime;

                if (timerText != null)
                {
                    timerText.text = (ROTATE_TIME_LIMIT - rotateCounter).ToString("F2");
                }
				break;

            case State.MOVE:
				// 操作キューブの移動
                float rate = moveCounter == 0.0f ? 0.0f : moveCounter / moveSpeed;
				cube.Move(rate);

                if (moveCounter >= moveSpeed)
                {
                    // 指定時間を越えたら操作パートに戻る
                    ResultAction();
					state = State.ROTATE;
				}
                else
                {
                    moveCounter += deltaTime;
                }

				break;
        }
	}

    // タイムリミット到達時の処理
	private void GoAction()
    {
        moveCounter = 0.0f;
		state = State.MOVE;
	}

    // 操作キューブの移動終了後の処理
    private void ResultAction()
    {
        rotateCounter = 0.0f;
        if (timerText != null)
        {
            timerText.text = ROTATE_TIME_LIMIT.ToString("F2");
        }
		SetAreaRotate();
		cube.EndMove();
    }

    // 障害物の回転をセット
    private void SetAreaRotate()
    {
		// -180度から180度の範囲でランダムな角度を決定
		Vector3 rotate = Vector3.zero;
		rotate.z = Random.Range(-180f, 180f);
		areaTransform.rotation = Quaternion.Euler(rotate);
	}
}
