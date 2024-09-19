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
    private Cube cube = null;                   // ���삷�闧����
    [SerializeField]
    private Transform areaTransform = null;     // ��Q��
	[SerializeField]
	private float rotateSpeed = 120.0f;          // ����L���[�u�̉�]���x�i/s�j
    [SerializeField]
    private float moveSpeed = 1.0f;             // ����L���[�u�̈ړ����ԁis)
    [SerializeField]
    private TextMeshProUGUI timerText = null;

	public State state { get; private set; } = State.ROTATE;

    private float rotateCounter = 0.0f;
    private float moveCounter = 0.0f;

    private readonly float ROTATE_TIME_LIMIT = 10.0f;   // ��]���쎞��

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
				// ����L���[�u�̈ړ�
                float rate = moveCounter == 0.0f ? 0.0f : moveCounter / moveSpeed;
				cube.Move(rate);

                if (moveCounter >= moveSpeed)
                {
                    // �w�莞�Ԃ��z�����瑀��p�[�g�ɖ߂�
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

    // �^�C�����~�b�g���B���̏���
	private void GoAction()
    {
        moveCounter = 0.0f;
		state = State.MOVE;
	}

    // ����L���[�u�̈ړ��I����̏���
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

    // ��Q���̉�]���Z�b�g
    private void SetAreaRotate()
    {
		// -180�x����180�x�͈̔͂Ń����_���Ȋp�x������
		Vector3 rotate = Vector3.zero;
		rotate.z = Random.Range(-180f, 180f);
		areaTransform.rotation = Quaternion.Euler(rotate);
	}
}
