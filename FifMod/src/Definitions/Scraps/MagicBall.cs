using System;
using System.Collections;
using System.Collections.Generic;
using LethalLib.Modules;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace FifMod.Definitions
{
    public class MagicBallProperties : FifModScrapProperties
    {
        public override string ItemAssetPath => "Scraps/MagicBall/MagicBallItem.asset";
        public override int Rarity => ConfigManager.ScrapsMagicBallRarity.Value;
        public override Levels.LevelTypes Moons => Levels.LevelTypes.All;

        public override Type CustomBehaviour => typeof(MagicBallBehaviour);
        public override Dictionary<string, string> Tooltips => new()
        {
            {"Item primary use", "Shake ball"}
        };
    }

    public class MagicBallBehaviour : GrabbableObject
    {
        private bool _canShake;
        private Transform _answerObject;
        private TMP_Text _answerText;
        private float _targetRotation;

        private AudioSource _audioSource;
        private AudioClip _shakeSound;
        private AudioClip _yesSound;
        private AudioClip _noSound;
        private AudioClip _maybeSound;

        public record struct Answer(string Message, AudioClip Audio, int Chance);
        private Answer[] _answers;

        public override void Start()
        {
            var d = FindObjectOfType<KepRemapPanel>(true).remappableKeys[8];
            FifMod.Logger.LogInfo(d.ControlName);
            FifMod.Logger.LogInfo(d.rebindingIndex);
            FifMod.Logger.LogInfo(d.currentInput.name);
            FifMod.Logger.LogInfo(d.currentInput.action.controls[0].name);

            _audioSource = GetComponent<AudioSource>();
            _shakeSound = FifMod.Assets.GetAsset<AudioClip>("Scraps/MagicBall/MagicBallShake.wav");
            _yesSound = FifMod.Assets.GetAsset<AudioClip>("Scraps/MagicBall/MagicBallYes.wav");
            _noSound = FifMod.Assets.GetAsset<AudioClip>("Scraps/MagicBall/MagicBallNo.wav");
            _maybeSound = FifMod.Assets.GetAsset<AudioClip>("Scraps/MagicBall/MagicBallMaybe.wav");

            _answers = new Answer[]
            {
                new("yes", _yesSound, 45),
                new("no", _noSound, 45),
                new("maybe", _maybeSound, 10)
            };

            grabbable = true;
            grabbableToEnemies = true;
            _canShake = true;

            _answerObject = transform.GetChild(0).GetChild(0);
            _answerText = _answerObject.GetComponentInChildren<TMP_Text>();

            ResetMagicBall();
            base.Start();
        }

        private void ResetMagicBall()
        {
            _answerObject.transform.localRotation = Quaternion.Euler(0, 90, 0);
            _targetRotation = 180;
            _answerText.text = "";
            ToggleAnswerText(false);
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            if (!playerHeldBy) return;
            if (_canShake) StartCoroutine(nameof(CO_ShakeBall));
        }

        public override void PocketItem()
        {
            base.PocketItem();
            ToggleAnswerText(false);
        }

        public override void EquipItem()
        {
            base.EquipItem();
            ToggleAnswerText(true);
        }

        private IEnumerator CO_ShakeBall()
        {
            _canShake = false;

            playerHeldBy.playerBodyAnimator.SetTrigger("shakeItem");
            PlayShakeServerRpc();
            MoveRotationServerRpc(UnityEngine.Random.Range(0, 2) == 0 ? -90 : 90);
            yield return new WaitForSeconds(0.4f);

            var random = UnityEngine.Random.Range(1, 101);
            var randomOffset = 0;
            for (int i = 0; i < _answers.Length; i++)
            {
                var answer = _answers[i];
                if (random <= answer.Chance + randomOffset)
                {
                    SyncRandomServerRpc(i);
                    break;
                }
                else randomOffset += answer.Chance;
            }

            yield return new WaitForSeconds(0.2f);
            _canShake = true;
        }

        [ServerRpc]
        private void PlayShakeServerRpc()
        {
            PlayShakeClientRpc();
        }

        [ClientRpc]
        private void PlayShakeClientRpc()
        {
            _audioSource.PlayOneShot(_shakeSound);
        }

        [ServerRpc]
        private void SyncRandomServerRpc(int choice)
        {
            SyncRandomClientRpc(choice);
        }

        [ClientRpc]
        private void SyncRandomClientRpc(int choice)
        {
            var answer = _answers[choice];
            _answerText.text = answer.Message;
            _audioSource.PlayOneShot(answer.Audio);
            MoveRotation(0);
        }

        [ServerRpc]
        private void MoveRotationServerRpc(float rotation)
        {
            MoveRotationClientRpc(rotation);
        }

        [ClientRpc]
        private void MoveRotationClientRpc(float rotation)
        {
            MoveRotation(rotation);
        }

        private void MoveRotation(float rotation)
        {
            _targetRotation = rotation;
        }

        private void ToggleAnswerText(bool enable)
        {
            _answerText.gameObject.SetActive(enable);
        }

        public override void Update()
        {
            base.Update();
            _answerObject.localRotation = Quaternion.Slerp(_answerObject.localRotation, Quaternion.Euler(0, _targetRotation, 0), Time.deltaTime * 5);
        }
    }
}