import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CpComponentsSelectorComponent } from './cp-components-selector.component';

describe('CpComponentsSelectorComponent', () => {
  let component: CpComponentsSelectorComponent;
  let fixture: ComponentFixture<CpComponentsSelectorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CpComponentsSelectorComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(CpComponentsSelectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
