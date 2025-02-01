using UnityEngine;
using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Playables;
using Demo.Scripts.Runtime.Item;

abstract public class ConsumableItem : FPSItem
{
    [Header("Animation Settings")]
    [SerializeField] protected FPSAnimationAsset useAnimation;
    [SerializeField] protected FPSAnimationAsset equipClip;
    [SerializeField] protected FPSAnimationAsset unEquipClip;
    
    // Core components
    protected Animator _controllerAnimator;
    protected IPlayablesController _playablesController;
    protected FPSAnimator _fpsAnimator;
    protected PlayerLoadout _playerLoadout;
    
    public override void OnEquip(GameObject parent)
    {
        if (parent == null) return;
        
        // Get all necessary components
        _controllerAnimator = parent.GetComponent<Animator>();
        _playablesController = parent.GetComponent<IPlayablesController>();
        _fpsAnimator = parent.GetComponent<FPSAnimator>();
        _playerLoadout = parent.GetComponent<PlayerLoadout>();
        
        // Set up animator controller if needed
        if (overrideController != _controllerAnimator.runtimeAnimatorController)
        {
            _playablesController.UpdateAnimatorController(overrideController);
        }
        
        _fpsAnimator.LinkAnimatorProfile(gameObject);

        // Play equip animation if available
        if (equipClip != null)
        {
            _playablesController.PlayAnimation(equipClip);
            return;
        }
        
        _fpsAnimator.LinkAnimatorLayer(equipMotion);
    }

    public override void OnUnEquip()
    {
        if (unEquipClip != null)
        {
            _playablesController.PlayAnimation(unEquipClip);
            return;
        }
        
        _fpsAnimator.LinkAnimatorLayer(unEquipMotion);
    }

    // Prevent default fire actions
    public override bool OnFirePressed() => false;
    public override bool OnFireReleased() => false;
    
    // New virtual methods for consumable-specific actions
    public virtual bool OnPrimaryUse()
    {
        if (_playerLoadout == null) return false;
        if (!_playerLoadout.HasCurrentItemUses()) return false;
        
        // Play use animation if available
        if (useAnimation != null)
        {
            _playablesController.PlayAnimation(useAnimation);
        }
        
        // Consume the item
        _playerLoadout.ConsumeCurrentItem();
        return true;
    }
    
    public virtual bool OnSecondaryUse()
    {
        if (_playerLoadout == null) return false;
        if (!_playerLoadout.HasCurrentItemUses()) return false;
        
        // Base implementation does nothing but still checks for uses
        return false;
    }
    
    protected virtual void OnUseComplete()
    {
        // Called when use animation completes
        // Can be overridden by derived classes
    }
} 