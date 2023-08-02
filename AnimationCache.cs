using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.Animations;


namespace Project.Lib {
    /// <summary>
    /// アニメーションClip管理クラス
    /// </summary>
    public class AnimationCache<T> {
		Dictionary<string, AnimationClip> animationDict_;
		AnimationClip[] cacheClip_ = null;
		//clip.nameで名前を取得すると一時メモリ確保されてGC発生するのでキャッシュを持たせる
		string[] clipName_ = null;
        /// <summary>
        /// アニメーションクリップを追加
        /// </summary>
        public AnimationCache(AnimationClip[] clipArray) {
            string[] motionNames = Enum.GetNames(typeof(T));
            cacheClip_ = new AnimationClip[motionNames.Length];
			animationDict_ = new Dictionary<string, AnimationClip>();

			//clip配列の順番を調整してenumの数値でclipを取得できるようにする
			for (int i = 0, max = clipArray.Length; i < max; i++) {
				animationDict_[clipArray[i].name] = clipArray[i];

				for (int j = 0, max2 = motionNames.Length; j < max2; j++) {
                    if (clipArray[i].name.EndsWith(motionNames[j])) {
                        cacheClip_[j] = clipArray[i];
                        break;
                    }
                }
            }

			//clip名も同様に順番をそろえた状態でキャッシュを作る
			clipName_ = new string[motionNames.Length];
			for (int i = 0, max = cacheClip_.Length; i < max; i++) {
				if(cacheClip_[i] != null){
					clipName_[i] = cacheClip_[i].name;
				}
			}

		}

		/// <summary>
		/// クリップ取得
		/// </summary>
		public AnimationClip GetClip(int index) {
	        return cacheClip_[index];
        }

		/// <summary>
		/// クリップ名を取得
		/// </summary>
		public string GetClipName(int index) {
			return clipName_[index];
		}

		/// <summary>
		/// クリップ取得
		/// </summary>
		public AnimationClip GetClip(string name) {
			return animationDict_[name];
		}

		/// <summary>
		/// アニメーションを所持しているかどうか？
		/// </summary>
		public bool IsExist(string name) {
			return animationDict_.ContainsKey(name);
		}


		/// <summary>
		/// アニメーションを追加
		/// </summary>
		public void AddClip(AnimationClip clip) {
			animationDict_[clip.name] = clip;
		}

	}
}
