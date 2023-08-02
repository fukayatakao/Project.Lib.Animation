using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace Project.Lib {
    //PlayableGraphの破棄タイミングを保つためにMonobehaviourを使う
    /// <summary>
    /// アニメーション管理クラス
    /// </summary>
    public abstract class PlayableAnimation : MonoBehaviour {
        Animator animator_;
		//Playable API
		PlayableGraph graph_;
        AnimationMixerPlayable mixer_;
        //現在再生中アニメーション
        const int CurrentIndex = 0;
        AnimationClipPlayable current_;


		//前再生アニメーション(クロスフェード用)
		const int PrevIndex = 1;
        AnimationClipPlayable prev_;
		bool requested_ = false; 
		AnimationClip changeRequest_;

		//アニメーションのクロスフェード用タスク
		CoroutineTask crossFade_ = new CoroutineTask();
		private float fadeTime_;
		public float FadeTime { get { return crossFade_.IsEnd() ? -1 : fadeTime_; } }


		private bool pause_;

		private bool isAlive_;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public virtual void Create() {
            //hideFlags = HideFlags.HideInInspector;
            graph_ = PlayableGraph.Create(gameObject.name);
			animator_ = gameObject.GetComponent<Animator>();
			Init();
			isAlive_ = true;
		}

		/// <summary>
		/// インスタンス破棄
		/// </summary>
		public virtual void Destroy() {
			graph_.Destroy();
			isAlive_ = false;
		}

		/// <summary>
		/// 完全に破棄されたときに解放忘れしてないかチェック
		/// </summary>
		void OnDestroy() {
			Debug.Assert(!isAlive_, "alread alive instance:" + GetType().ToString());
		}

		/// <summary>
		/// 初期化
		/// </summary>
		private void Init() {
            // AnimationClipをMixerに登録
            //current_ = AnimationClipPlayable.Create(graph_, cache_.GetClip((int)defaultMotion));
            current_ = AnimationClipPlayable.Create(graph_, null);
            mixer_ = AnimationMixerPlayable.Create(graph_, 2);
            mixer_.ConnectInput(CurrentIndex, current_, 0);
            mixer_.SetInputWeight(CurrentIndex, 1f);
            // outputにmixerとanimatorを登録して、再生
            var output = AnimationPlayableOutput.Create(graph_, "output", animator_);
            
            output.SetSourcePlayable(mixer_);
			graph_.SetTimeUpdateMode(DirectorUpdateMode.Manual);
			graph_.Play();
            pause_ = false;
			requested_ = false;
		}
		/// <summary>
		/// 初期モーションをセット
		/// </summary>
		public void InitPlay(AnimationClip clip) {
			prev_ = AnimationClipPlayable.Create(graph_, clip);
			current_ = AnimationClipPlayable.Create(graph_, clip);
			graph_.Disconnect(mixer_, CurrentIndex);
			graph_.Disconnect(mixer_, PrevIndex);

			mixer_.ConnectInput(PrevIndex, prev_, 0);
			mixer_.ConnectInput(CurrentIndex, current_, 0);

			mixer_.SetInputWeight(PrevIndex, 0f);
			mixer_.SetInputWeight(CurrentIndex, 1f);

			graph_.Evaluate(0);
		}

		/// <summary>
		/// 実行処理
		/// </summary>
		public void LateExecute() {
			crossFade_.Execute();
			graph_.Evaluate(Time.deltaTime);
		}
		/// <summary>
		/// アニメーション再生
		/// </summary>
		public void Play(AnimationClip clip, float crossFadeTime = 0.25f) {
			//同一フレームで何度も再生を実行されたときの対策
			if (!requested_) {
				// 古いアニメーションを破棄し、次に再生するアニメーションを登録する
				if (prev_.IsValid())
					prev_.Destroy();
				prev_ = current_;
				requested_ = true;
			}
			current_ = AnimationClipPlayable.Create(graph_, clip);
			changeRequest_ = clip;
			crossFade_.Play(CrossFade(clip, crossFadeTime));
		}

		/// <summary>
		/// クロスフェードの準備
		/// </summary>
		private void InitCrossFade(AnimationClip clip) {
			// ClipPlayableを上書きは出来ない為、一旦mixerの1番と2番を一旦アンロード
			graph_.Disconnect(mixer_, CurrentIndex);
			graph_.Disconnect(mixer_, PrevIndex);

			mixer_.ConnectInput(PrevIndex, prev_, 0);
			mixer_.ConnectInput(CurrentIndex, current_, 0);
			mixer_.SetInputWeight(PrevIndex, 1f);
			mixer_.SetInputWeight(CurrentIndex, 0f);

			requested_ = false;
			changeRequest_ = null;
		}

		/// <summary>
		/// アニメーションをフェード切り替え
		/// </summary>
		private IEnumerator CrossFade(AnimationClip clip, float fadeTime) {
			InitCrossFade(clip);
			float transitionTime = Time.time + fadeTime;
			while (Time.time < transitionTime) {
				if (pause_) {
					yield return null;
					continue;
				}
				float t = (transitionTime - Time.time) / fadeTime;
				mixer_.SetInputWeight(PrevIndex, t);
				mixer_.SetInputWeight(CurrentIndex, 1f - t);
				yield return null;
			}

			mixer_.SetInputWeight(PrevIndex, 0f);
			mixer_.SetInputWeight(CurrentIndex, 1f);
		}
		/// <summary>
		/// アニメーション一時停止
		/// </summary>
		public void Pause() {
			prev_.Pause();
			current_.Pause();
            pause_ = true;
        }
        /// <summary>
        /// アニメーション一時停止
        /// </summary>
        public void Resume() {
            pause_ = false;
            /*if (prev_.IsValid())
                prev_.Play();
            current_.Play();*/
        }
		/// <summary>
		/// アニメーション再生中か
		/// </summary>
		public bool IsPlay(string clipName) {
			if (IsEnd())
				return false;
            return current_.GetAnimationClip().name == clipName;
		}

        /// <summary>
        /// アニメーション終了チェック
        /// </summary>
        public bool IsEnd() {
			return current_.GetTime() > current_.GetAnimationClip().length;
		}
        /// <summary>
        /// ループアニメーションか
        /// </summary>
        public bool IsLoop() {
			return current_.GetAnimationClip().isLooping;
        }
      /*/// <summary>
        /// アニメーションを所持しているかどうか？
        /// </summary>
        public bool IsExist(string animationName) {
            return true;
        }*/

        public void SetSpeed(float speed) {
            graph_.GetRootPlayable(0).SetSpeed(speed);
        }
        public void SetTime(float t) {
            current_.SetTime(t);


		}
        public float GetTime() {
			return (float)current_.GetTime();
        }
		public float GetPrevTime() {
			return (float)prev_.GetTime();
		}
		public float GetLength() {
            return current_.GetAnimationClip().length;
        }
		public string GetCurrentClipname(){
			return current_.GetAnimationClip().name;
		}
		public string GetPrevClipname() {
			return prev_.GetAnimationClip().name;
		}
    }
}
