using System;
using System.Collections;
using System.Collections.Generic;
using FifMod.Base;
using FifMod.Utils;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace FifMod.Definitions
{
    public class MagicBallProperties : FifModScrapProperties
    {
        public override string ItemAssetPath => "Scraps/MagicBall/MagicBallItem.asset";
        public override FifModRarity Rarity => FifModRarity.All(45 * ConfigManager.ScrapsMagicBallRarity.Value);
        public override MoonFlags Moons => MoonFlags.All;

        public override Type CustomBehaviour => typeof(MagicBallBehaviour);
        public override Dictionary<string, string> Tooltips => new()
        {
            {"Item primary use", "Shake ball"}
        };

        public override int Weight => 1;
        public override int MinValue => 45;
        public override int MaxValue => 85;
        public override ScrapSpawnFlags SpawnFlags => ScrapSpawnFlags.All;
    }

    public class MagicBallBehaviour : GrabbableObject
    {
        private bool _canShake;
        private Transform _answerObject;
        private TMP_Text _answerText;
        private float _targetRotation;

        private AudioSource _audioSource;
        private AudioSource _answerSource;
        private AudioClip _shakeSound;
        private AudioClip _yesSound;
        private AudioClip _noSound;
        private AudioClip _maybeSound;

        public record struct Answer(string Message, AudioClip Audio, int Chance);
        private Answer[] _answers;

        private int _instabilityLevel;

        public override void Start()
        {
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

            _answerObject = gameObject.GetChild("Answer").transform;
            _answerText = _answerObject.GetComponentInChildren<TMP_Text>();
            _answerSource = _answerObject.GetComponent<AudioSource>();

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
            _answerSource.PlayOneShot(answer.Audio);
            MoveRotation(0);

            if (IsOwner && !StartOfRound.Instance.inShipPhase)
            {
                StopCoroutine(nameof(CO_IncreaseInstability));
                StartCoroutine(nameof(CO_IncreaseInstability));
            }
        }

        private IEnumerator CO_IncreaseInstability()
        {
            IncreaseInstabilityServerRpc();

            yield return new WaitForSeconds(1f);
            SetInstabilityServerRpc(0);
        }

        [ServerRpc]
        private void IncreaseInstabilityServerRpc()
        {
            _instabilityLevel++;
            SetInstabilityClientRpc(_instabilityLevel);

            if (_instabilityLevel >= 4)
            {
                var rand = UnityEngine.Random.Range(0, 30);
                if (rand <= _instabilityLevel / 2)
                {
                    ExplodeClientRpc();
                }
            }
        }

        [ClientRpc]
        private void ExplodeClientRpc()
        {
            FifModUtils.CreateExplosion(transform.position, true, 200, enemyHitForce: 10);
            Destroy(gameObject);
        }

        [ServerRpc]
        private void SetInstabilityServerRpc(int value)
        {
            SetInstabilityClientRpc(value);
        }

        [ClientRpc]
        private void SetInstabilityClientRpc(int value)
        {
            _instabilityLevel = value;

            if (_instabilityLevel >= 4)
            {
                _answerSource.pitch += (float)_instabilityLevel / 100 * 2;
            }
            else if (_instabilityLevel == 0)
            {
                _answerSource.pitch = 1;
            }
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