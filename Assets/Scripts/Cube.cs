using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class Cube : Agent
{
	// 連続アクション取得用インデックス定義
	public enum ContinuousActionIndex
	{
		INPUT_AXIS_VERTICAL,        // X軸回転：上、下キー押下取得用
		INPUT_AXIS_HORIZONTAL       // Z軸回転：左、右キー押下取得用
	}

	[SerializeField]
	private Vector3 startPos = Vector3.zero;                // 開始位置
	[SerializeField]
	private Vector3 goalPos = Vector3.zero;                 // ゴール位置
	[SerializeField]
	private LayerMask hitLayers;                            // ヒットさせるレイヤーを指定
	[SerializeField]
	private GameObject explosionPrefab = null;

	public bool IsCollide { get; private set; } = false;    // 衝突フラグ

	private new Rigidbody rigidbody = null;
	private BoxCollider boxCollider = null;
	private float rotateSpeed = 0.0f;				// 回転速度(/s)
	private bool isMoving = false;					// 移動中フラグ
	private float rayLength = 200.0f;					// レイの長さ
	private Vector3[] vecList = new Vector3[8];
	byte hitRayFlags = 0;

	public override void Initialize()
	{
		rigidbody = GetComponent<Rigidbody>();
		boxCollider = GetComponent<BoxCollider>();
	}

	public bool IsHitRay()
	{
		return hitRayFlags != (byte)0xFF;
	}

	// エピソードの開始時に呼ばれる
	public override void OnEpisodeBegin()
	{
		// 衝突後の慣性をリセットする
		rigidbody.velocity = Vector3.zero;
		rigidbody.angularVelocity = Vector3.zero;

		// 初期位置に戻す
		transform.localPosition = startPos;

		// -180度から180度の範囲でランダムな角度を決定
		Vector3 rotate = Vector3.zero;
		rotate.x = Random.Range(-180f, 180f);
		rotate.y = Random.Range(-180f, 180f);
		rotate.z = Random.Range(-180f, 180f);
		transform.rotation = Quaternion.Euler(rotate);

		// フラグをリセット
		IsCollide = false;
		isMoving = false;
		hitRayFlags = 0;
	}

	// エージェントが環境から観測データを収集するために毎フレーム呼ばれるメソッド
	public override void CollectObservations(VectorSensor sensor)
	{
		sensor.AddObservation(transform.rotation);
		sensor.AddObservation(vecList[0]);
		sensor.AddObservation(vecList[1]);
		sensor.AddObservation(vecList[2]);
		sensor.AddObservation(vecList[3]);
		sensor.AddObservation(vecList[4]);
		sensor.AddObservation(vecList[5]);
		sensor.AddObservation(vecList[6]);
		sensor.AddObservation(vecList[7]);
		sensor.AddObservation(hitRayFlags);
	}

	// エージェントが環境から行動を受け取ったときに呼び出されるメソッド
	public override void OnActionReceived(ActionBuffers actionBuffers)
	{
		if (isMoving)
		{
			// 移動中なら処理を抜ける
			return;
		}

		vecList = GetBoxCollideVertices(boxCollider);
		byte flags = (byte)0xFF;
		int hitCount = 0;
		for (int j = 0; j < vecList.Length; j++)
		{
			// レイを可視化（デバッグ用）
			//Debug.DrawRay(vecList[j], Vector3.forward * rayLength, Color.red);

			// レイキャストを実行
			RaycastHit hit;
			if (Physics.Raycast(vecList[j], Vector3.forward, out hit, rayLength, hitLayers))
			{
				flags &= (byte)~(1 << j);
				hitCount++;
			}
		}

		hitRayFlags = flags;

		switch (hitCount)
		{
			case 0: SetReward(1.40f);  break;
			case 1: SetReward(1.10f); break;
			case 2: SetReward(0.80f); break;
			case 3: SetReward(0.50f); break;
			case 4: SetReward(-0.10f); break;
			case 5: SetReward(-0.20f); break;
			case 6: SetReward(-0.30f); break;
			case 7: SetReward(-0.40f); break;
			case 8: SetReward(-0.50f); break;
		}

		// 連続アクションを取得
		var continuousActionsOut = actionBuffers.ContinuousActions;
		float deltaTime = Time.fixedDeltaTime;

		// 世界のX軸を軸に回転する(-1.0f~1.0f)
		float verticalDir = continuousActionsOut[(int)ContinuousActionIndex.INPUT_AXIS_VERTICAL];
		if (verticalDir < -0.330f)
		{
			// 世界のX軸を軸に時計周りに回転
			transform.Rotate(Vector3.left * rotateSpeed * deltaTime, Space.World);
		}
		else if (verticalDir > 0.330f)
		{
			// 世界のX軸を軸に半時計周りに回転
			transform.Rotate(Vector3.right * rotateSpeed * deltaTime, Space.World);
		}

		// 世界のz軸を軸に回転する(-1.0f~1.0f)
		float horizontalDir = continuousActionsOut[(int)ContinuousActionIndex.INPUT_AXIS_HORIZONTAL];
		if (horizontalDir < -0.330f)
		{
			// 世界のz軸を軸に時計周りに回転
			transform.Rotate(Vector3.back * rotateSpeed * deltaTime, Space.World);
		}
		else if (horizontalDir > 0.330f)
		{
			// 世界のz軸を軸に半時計周りに回転
			transform.Rotate(Vector3.forward * rotateSpeed * deltaTime, Space.World);
		}
	}

	public override void Heuristic(in ActionBuffers actionsOut)
	{
		var continuousActionsOut = actionsOut.ContinuousActions;
		continuousActionsOut[(int)ContinuousActionIndex.INPUT_AXIS_VERTICAL] = Input.GetAxis("Vertical");
		continuousActionsOut[(int)ContinuousActionIndex.INPUT_AXIS_HORIZONTAL] = Input.GetAxis("Horizontal");
	}

	// 目標地点に向かって移動する処理
	public void Move(float rate)
	{
		if (rate == 0.0f)
		{
			return;
		}

		if (IsCollide)
		{
			// 衝突していたら移動しない
			return;
		}

		Vector3 distance = goalPos - startPos;
		Vector3 pos = startPos;
		pos += distance * rate;
		transform.localPosition = pos;
	}

	// 移動終了時の処理
	public void EndMove()
	{
		if (IsCollide)
		{
			// 障害物に衝突した場合、ペナルティを設定
			//Debug.Log("失敗！！！ 衝突しました");
			SetReward(-1.0f);
		}
		else
		{
			// 障害物に衝突しなかった場合、報酬を設定
			//Debug.Log("成功！！！ 衝突しませんでした");
			SetReward(1.0f);
		}

		EndEpisode();
	}

	void OnCollisionEnter(Collision collision)
	{
		if (IsCollide)
		{
			return;
		}

		IsCollide = true;

		if (explosionPrefab != null)
		{
			// 爆発エフェクト
			Instantiate(explosionPrefab, collision.contacts[0].point, Quaternion.identity);
		}
	}

	public void SetRotateSpeed(float speed)
	{
		rotateSpeed = speed;
	}

	private Vector3[] GetBoxCollideVertices(BoxCollider Col)
	{
		Transform trs = Col.transform;
		Vector3 sc = trs.lossyScale;

		sc.x *= Col.size.x;
		sc.y *= Col.size.y;
		sc.z *= Col.size.z;

		sc *= 0.5f;

		Vector3 cp = trs.TransformPoint(Col.center);

		Vector3 vx = trs.right * sc.x;
		Vector3 vy = trs.up * sc.y;
		Vector3 vz = trs.forward * sc.z;

		Vector3 p1 = -vx + vy + vz;
		Vector3 p2 = vx + vy + vz;
		Vector3 p3 = vx + -vy + vz;
		Vector3 p4 = -vx + -vy + vz;

		Vector3[] vertices = new Vector3[8];

		vertices[0] = cp + p1;
		vertices[1] = cp + p2;
		vertices[2] = cp + p3;
		vertices[3] = cp + p4;

		vertices[4] = cp - p1;
		vertices[5] = cp - p2;
		vertices[6] = cp - p3;
		vertices[7] = cp - p4;

		return vertices;
	}
}
