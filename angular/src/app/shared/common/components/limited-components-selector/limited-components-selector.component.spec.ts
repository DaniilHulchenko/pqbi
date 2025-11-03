import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LimitedComponentsSelectorComponent } from './limited-components-selector.component';

describe('LimitedComponentsSelectorComponent', () => {
  let component: LimitedComponentsSelectorComponent;
  let fixture: ComponentFixture<LimitedComponentsSelectorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LimitedComponentsSelectorComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(LimitedComponentsSelectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
