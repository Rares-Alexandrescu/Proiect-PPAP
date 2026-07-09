import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditCompanie } from './edit-companie';

describe('EditCompanie', () => {
  let component: EditCompanie;
  let fixture: ComponentFixture<EditCompanie>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EditCompanie],
    }).compileComponents();

    fixture = TestBed.createComponent(EditCompanie);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
